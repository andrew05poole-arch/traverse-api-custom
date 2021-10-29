#region Using Directives
using System;
using System.Threading.Tasks;
using TRAVERSE.Business;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.Business
{
    public static class ApiEntityProviderExtension
    {
        #region Static Methods
        [Obsolete("Function has been deprecated and performs no activity")]
        /// <summary>
        /// Extension method to add paging to provider. Using this paging method will replace the standard dataprovider using reflection for data loads
        /// </summary>
        /// <param name="providerBase"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        public static void SetPage(this IProvider providerBase, int pageNumber, int pageSize = 50)
        { }

        //Asynchronous operation for SetPage method
        [Obsolete("Function has been deprecated and performs no activity")]
        public static async Task SetPageAsync(this IProvider providerBase, int pageNumber, int pageSize = 50)
        { }

        public static async Task<EntityList<E>> Load<E>(this IProvider providerBase, string compId, int pageNumber, int pageSize = 50)
            where E : EntityBase
        {
            return await Load<E>(providerBase, compId, null, pageNumber, pageSize);
        }

        public static async Task<EntityList<E>> Load<E>(this IProvider providerBase, string compId, FilterCriteria criteria, int pageNumber, int pageSize = 50)
            where E : EntityBase
        {
            if (providerBase == null)
                return null;

            FilterCriteria filter = criteria != null ? criteria : new FilterCriteria();

            providerBase.CompId = compId;
            await Task.Run(() => providerBase.LoadList(compId, filter));

            return (EntityList<E>)providerBase.ItemList;
        }
        #endregion Static Methods
    }
}
