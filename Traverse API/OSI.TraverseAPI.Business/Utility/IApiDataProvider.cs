using TRAVERSE.Business;

namespace OSI.TraverseApi.Business
{
    public interface IApiDataProvider
    {
        IDataProvider DataProvider { get; set; }

        string TableName { get; set; }

        string[] KeyColumns { get; set; }

        int PageSize { get; set; }

        int PageNumber { get; set; }
    }
}
