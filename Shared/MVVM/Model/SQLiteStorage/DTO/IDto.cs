namespace Shared.MVVM.Model.SQLiteStorage.DTO
{
    public interface IDto<out RepositoryKeyT>
    {
        RepositoryKeyT GetRepositoryKey();
    }
}
