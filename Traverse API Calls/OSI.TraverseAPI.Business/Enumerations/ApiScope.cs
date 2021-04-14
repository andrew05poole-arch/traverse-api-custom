namespace OSI.TraverseApi.Business
{
    public enum ApiScope : byte
    {
        None = 0,
        AllowRead = 1,
        AllowEdit = 2,
        AllowNew = 4,
        AllowDelete = 8
    }
}
