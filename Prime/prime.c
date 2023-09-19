#include <Windows.h>
#include <bcrypt.h>
#include <stdio.h>
#include "mpir.h"

/* w C++ musi byc tez extern "C", bo C# poprzez DllImport moze wywolywac tylko funkcje
o niemanglowanych nazwach, a w C++ przez obsluge przeciazania funkcji nazwy sa manglowane */
#define EXPORT __declspec(dllexport)

#pragma comment (lib, "bcrypt.lib")

NTSTATUS generate_random(UINT8* bytes, UINT32 length) // ULONG (unsigned long) ma 4 B tak jak unsigned int
{
    BCRYPT_ALG_HANDLE alg = NULL;
    NTSTATUS status = NTE_FAIL;
    status = BCryptOpenAlgorithmProvider(&alg, L"RNG", NULL, 0);
    if (status != ERROR_SUCCESS) return status;
    status = BCryptGenRandom(alg, (PUCHAR)bytes, (ULONG)length, 0);
    if (alg != NULL) BCryptCloseAlgorithmProvider(alg, 0);
    return status;
}

int random_seed_prng(gmp_randstate_t* result)
{
    UINT8 bytes[sizeof(mpir_ui)];
    if (!BCRYPT_SUCCESS(generate_random(bytes, sizeof(mpir_ui)))) return -1;
    gmp_randinit_default(*result);
    gmp_randseed_ui(*result, *((mpir_ui*)bytes)); // seed jest typu unsigned long long
    return 0;
}

/* EXPORT int generate_probable_prime(unsigned int length_bytes, char** decimal_digits, size_t* decimal_length)
{
    if (length_bytes == 0)
    {
        *decimal_length = 0;
        *decimal_digits = (char*)malloc(1 * sizeof(char)); // tylko \0
        return 0;
    }
    BYTE *bytes = (BYTE*)malloc(length_bytes);
    if (!BCRYPT_SUCCESS(generate_random(bytes, length_bytes))) return -1;
    bytes[0] = bytes[0] | 0b00000001; // ustawiamy pierwszy bit liczby na 1, aby byla nieparzysta
    mpz_t number;
    mpz_init(number);
    mpz_import(number, length_bytes, -1, sizeof(BYTE), 0, 0, bytes); // budujemy liczbe calkowita z bajtow
    free(bytes);
    gmp_randstate_t rng; // generator pseudolosowy (z prawdziwie losowym seedem)
    // do algorytmu testu pierwszosci Millera-Rabina
    if (random_seed_prng(&rng) != 0) return -2;
    while (1) // zwiekszamy liczbe (nieparzysta) o 2 dopoki nie trafimy na liczbe prawdopodobnie pierwsza
    {
        if (mpz_probable_prime_p(number, rng, 100, 0) == 0) // na pewno zlozona
            mpz_add_ui(number, number, 2); // zwiekszamy liczbe o 2
        else break; // prawdopodobnie (nie na pewno) pierwsza
    }
    // tworzymy stringa w postaci dziesietnej
    *decimal_length = mpz_sizeinbase(number, 10);
    *decimal_digits = (char*)malloc((*decimal_length + 1) * sizeof(char)); // +1, bo mpz_get_str wstawia \0 na koncu,
    // ale nie trzeba dawac +2 (miejsce na ewentualny -), bo mamy tylko liczby dodatnie
    mpz_get_str(*decimal_digits, 10, number);
    mpz_clear(number);
    return 0;
} */

EXPORT int min_length_probable_prime(UINT32 min_length, UINT8** bytes,
    UINT32* final_length)
{
    *final_length = 0;
    *bytes = NULL;
    if (min_length == 0) return 0;
    UINT8* random_bytes = (UINT8*)malloc(min_length);
    if (!BCRYPT_SUCCESS(generate_random(random_bytes, min_length))) return -1;
    random_bytes[0] |= 0b00000001; // ustawiamy pierwszy bit liczby na 1, aby byla nieparzysta
    mpz_t number;
    mpz_init(number);
    // budujemy liczbe calkowita z bajtow
    mpz_import(number, min_length, -1, sizeof(UINT8), 0, 0, random_bytes);
    free(random_bytes);
    gmp_randstate_t rng; // generator pseudolosowy (z prawdziwie losowym seedem)
    // do algorytmu testu pierwszosci Millera-Rabina
    if (random_seed_prng(&rng) != 0) return -2;
    while (1) // zwiekszamy liczbe (nieparzysta) o 2 dopoki nie trafimy na liczbe prawdopodobnie pierwsza
    {
        if (mpz_probable_prime_p(number, rng, 100, 0) == 0) // na pewno zlozona
            mpz_add_ui(number, number, 2); // zwiekszamy liczbe o 2
        else break; // prawdopodobnie (nie na pewno) pierwsza
    }
    // tworzymy tablice bajtow
    *final_length = (UINT32)mpz_sizeinbase(number, 256);
    if (*final_length == 0) return -3; // blad, bo min_byte_length na pewno bylo > 0
    *bytes = (UINT8*)malloc(*final_length);
    size_t written_bytes = 0;
    mpz_export(*bytes, &written_bytes, -1, sizeof(BYTE), 0, 0, number);
    if ((UINT32)written_bytes != *final_length)
    {
        free(*bytes);
        *bytes = NULL;
        *final_length = 0;
        return -4;
    }
    /* const decimal_length = mpz_sizeinbase(number, 10);
    char* decimal_digits = malloc(decimal_length + 1);
    mpz_get_str(decimal_digits, 10, number);
    printf("%s\n", decimal_digits);
    free(decimal_digits); */
    mpz_clear(number);
    return 0;
}

int bounded_random(mpz_t range_mpz, mpz_t random_mpz)
{
    // https://www.pcg-random.org/posts/bounded-rands.html
    /* uint32_t x, r;
    do {
        x = rng();
        r = x % range;
    } while (x - r > (-range));
    return r; */
    const UINT32 x_length = (UINT32)mpz_sizeinbase(range_mpz, 256);
    UINT8* x_bytes = (UINT8*)malloc(x_length);
    mpz_t x_mpz, r_mpz, x_minus_r_mpz, minus_range_mpz;

    mpz_inits(x_mpz, r_mpz, x_minus_r_mpz, minus_range_mpz, NULL);
    mpz_neg(minus_range_mpz, range_mpz);
    int cmp;
    do {
        if (!BCRYPT_SUCCESS(generate_random(x_bytes, x_length)))
        {
            free(x_bytes);
            mpz_clears(x_mpz, r_mpz, x_minus_r_mpz, minus_range_mpz, NULL);
            return -1;
        }
        // budujemy liczbe calkowita z bajtow
        mpz_import(x_mpz, x_length, -1, sizeof(UINT8), 0, 0, x_bytes);
        mpz_mod(r_mpz, x_mpz, range_mpz);
        mpz_sub(x_minus_r_mpz, x_mpz, r_mpz);
        cmp = mpz_cmp(x_minus_r_mpz, minus_range_mpz);
    } while (cmp > 0);
    free(x_bytes);
    /* size_t written_bytes = 0;
    mpz_export(*random_bytes, &written_bytes, endian, sizeof(BYTE), 0, 0, r_mpz); */
    mpz_clears(minus_range_mpz, x_minus_r_mpz, x_mpz, NULL);
    mpz_set(random_mpz, r_mpz);
    mpz_clear(r_mpz);
    return 0;
}

EXPORT int probable_prime_between(UINT8* min_bytes, UINT32 min_length,
    UINT8* max_bytes, UINT32 max_length, INT8 endian,
    UINT8** prime_bytes, UINT32* prime_length)
{
    mpz_t range_mpz, min_mpz, max_mpz, temp_mpz;
    gmp_randstate_t rng; // generator pseudolosowy (z prawdziwie losowym seedem)
    // do algorytmu testu pierwszosci Millera-Rabina
    size_t written_bytes;

    *prime_length = 0;
    *prime_bytes = NULL;
    if (min_length == 0) return -1;
    if (max_length == 0) return -2;
    mpz_inits(range_mpz, min_mpz, max_mpz, NULL);
    /* 0-wy bit endian odpowiada za kolejnosc bajtow w min, a 1-szy w max:
    0 - little-endian
    1 - big-endian */
    // -1 + 2 * (endian & 0x01)
    // (endian & 0x01) == 0 ? -1 : 1
    mpz_import(max_mpz, max_length, endian, sizeof(UINT8), 0, 0, max_bytes);
    mpz_import(min_mpz, min_length, endian, sizeof(UINT8), 0, 0, min_bytes);
    mpz_sub(range_mpz, max_mpz, min_mpz);
    mpz_init(temp_mpz);
    if (bounded_random(range_mpz, temp_mpz) < 0)
    {
        mpz_clears(range_mpz, min_mpz, max_mpz, temp_mpz, NULL);
        return -3;
    }
    mpz_add(temp_mpz, temp_mpz, min_mpz);
    mpz_clears(range_mpz, min_mpz, NULL);
    if (mpz_odd_p(temp_mpz) == 0)
        mpz_add_ui(temp_mpz, temp_mpz, 1);
    if (random_seed_prng(&rng) != 0)
    {
        mpz_clears(max_mpz, temp_mpz, NULL);
        return -4;
    }
    while (1) // zwiekszamy liczbe (nieparzysta) o 2 dopoki nie trafimy na liczbe prawdopodobnie pierwsza
    {
        int cmp = mpz_cmp(temp_mpz, max_mpz);
        if (cmp > 0)
        {
            mpz_clears(max_mpz, temp_mpz, NULL);
            return 1;
        }
        if (mpz_probable_prime_p(temp_mpz, rng, 100, 0) == 0) // na pewno zlozona
            mpz_add_ui(temp_mpz, temp_mpz, 2); // zwiekszamy liczbe o 2
        else break; // prawdopodobnie (nie na pewno) pierwsza
    }
    // tworzymy tablice bajtow
    *prime_length = (UINT32)mpz_sizeinbase(temp_mpz, 256);
    if (*prime_length == 0)
    {
        mpz_clears(max_mpz, temp_mpz, NULL);
        return -5;
    }
    *prime_bytes = (UINT8*)malloc(*prime_length);
    mpz_export(*prime_bytes, &written_bytes, endian, sizeof(UINT8), 0, 0, temp_mpz);
    // nigdy nie zdarza sie, ze written_bytes jest wieksze niz maksymalna wartosc UINT32, wiec mozemy zrzutowac
    if ((UINT32)written_bytes != *prime_length)
    {
        free(*prime_bytes);
        *prime_bytes = NULL;
        *prime_length = 0;
        mpz_clears(max_mpz, temp_mpz, NULL);
        return -6;
    }
    mpz_clears(max_mpz, temp_mpz, NULL);
    return 0;
}

EXPORT int first_probable_prime_greater_or_equal(UINT8* min_bytes, UINT32 min_length, INT8 endian,
    UINT8** prime_bytes, UINT32* prime_length)
{
    mpz_t temp_mpz;
    gmp_randstate_t rng;
    size_t written_bytes;

    *prime_length = 0;
    *prime_bytes = NULL;
    if (min_length == 0) return -1;
    mpz_init(temp_mpz);
    mpz_import(temp_mpz, min_length, endian, sizeof(UINT8), 0, 0, min_bytes);
    if (mpz_odd_p(temp_mpz) == 0)
        mpz_add_ui(temp_mpz, temp_mpz, 1);
    if (random_seed_prng(&rng) != 0)
    {
        mpz_clear(temp_mpz);
        return -2;
    }
    while (1)
    {
        if (mpz_probable_prime_p(temp_mpz, rng, 100, 0) == 0)
            mpz_add_ui(temp_mpz, temp_mpz, 2);
        else break;
    }
    *prime_length = (UINT32)mpz_sizeinbase(temp_mpz, 256);
    if (*prime_length == 0) return -3;
    *prime_bytes = (UINT8*)malloc(*prime_length);
    mpz_export(*prime_bytes, &written_bytes, endian, sizeof(UINT8), 0, 0, temp_mpz);
    if ((UINT32)written_bytes != *prime_length)
    {
        free(*prime_bytes);
        *prime_bytes = NULL;
        *prime_length = 0;
        mpz_clear(temp_mpz);
        return -4;
    }
    mpz_clear(temp_mpz);
    return 0;
}

EXPORT void free_unmanaged(void* array)
{
    free(array);
}

/* EXPORT int is_probable_prime(char* decimal_digits, int* result)
{
    *result = 0; // false
    mpz_t number;
    mpz_init(number);
    if (mpz_set_str(number, decimal_digits, 10) == -1) return -1; // nie udalo sie sparsowac liczby
    gmp_randstate_t rng;
    if (random_seed_prng(&rng) != 0) return -2;
    *result = mpz_probable_prime_p(number, rng, 100, 0) != 0; // jezeli 0, to na pewno zlozona
    mpz_clear(number);
    return 0;
} */

EXPORT int is_probable_prime(UINT8* bytes, UINT32 length, INT8 endian, int* result)
{
    *result = 0; // false
    mpz_t number_mpz;
    mpz_init(number_mpz);
    mpz_import(number_mpz, length, endian, sizeof(UINT8), 0, 0, bytes);
    gmp_randstate_t rng;
    if (random_seed_prng(&rng) != 0)
    {
        mpz_clear(number_mpz);
        return -1;
    }
    // jezeli 0, to na pewno zlozona
    *result = mpz_probable_prime_p(number_mpz, rng, 100, 0) != 0;
    mpz_clear(number_mpz);
    return 0;
}

/* // kod do testowania biblioteki
int main()
{
    // BYTE bytes[sizeof(unsigned int)];
    unsigned int i;
    char* bytes;
    unsigned int byte_length;
    for (i = 0; i < 10; ++i)
    {
        /* if (!BCRYPT_SUCCESS(generate_random(bytes, sizeof(unsigned int)))) return -1;
        printf("%ui\n", *(unsigned int*)bytes); * /
        generate_probable_prime(1024 / 8, &bytes, &byte_length);
        for (unsigned int b = 0; b < byte_length; ++b)
            printf("%i ", bytes[b]);
        int is_prime = 0;
        is_probable_prime(bytes, byte_length, &is_prime);
        free(bytes);
        printf("\n");
        printf(is_prime == 0 ? "not prime" : "prime");
        printf("\n\n");
    }
    for (i = 0; i < 100; ++i)
    {
        int is_prime = 0;
        is_probable_prime(&i, 1, &is_prime);
        printf("%i ", i);
        printf(is_prime == 0 ? "not prime" : "prime");
        printf("\n");
    }
    return 0;
} */
