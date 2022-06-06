#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.CompanySetup;
using TRAVERSE.Core;
using TraverseApi;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.CompanySetup.Controllers
{
    public class ApiSmFormNumberController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "formnumber/{id?}", typeof(FormNumber))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "formnumber/{id?}", typeof(FormNumber))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "formnumber/{id}/increment", typeof(FormNumber))]
        public async Task<IHttpActionResult> Increment([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates(){}
        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            base.ProcessCustomResponse(args);
            if (args.Entity is FormNumber formNumber)
            {
                if (StringHelper.AreEqual(args.FieldName, FormNumberBase.Columns.NextNum.ToString(), false))
                {
                    if(CurrentNextNumber != 0)
                        args.ActualValue = CurrentNextNumber;
                }
            }
        }
        #endregion Overrides

        protected virtual async Task<EntityList<FormNumber>> Load(string id)
        {
            if (this.Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !this.Provider.Items.Exists(i => StringHelper.AreEqual(id, i.FormId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<FormNumber>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<FormNumberBase.Columns>();
                    builder.AppendEquals(FormNumberBase.Columns.FormId, id.ToString());
                    var list = new FormNumberProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    this.Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(this.Provider.Items, FunctionID);
            }

            return this.Provider.Items;
        }

        protected virtual async Task<FormNumber> Find(string id)
        {
            var list = await Load(id);
            return list?.Find(x => StringHelper.AreEqual(x.FormId, id, false));
        }

        protected virtual async Task<List<FormNumber>> ProcessEditRequest(bool isIncrement, dynamic body, string id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Form Number ID is provided along with more than one record.");

            if (list.Length > 1 && isIncrement)
                throw new InvalidValueException("Only one form number can be increased per request");

            var entityList = new List<FormNumber>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isIncrement, item, id);                
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await ValidateEntityListAsync(entityList);            
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<FormNumber> ProcessBodyItem(bool isIncrement, dynamic bodyItem, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.FormId) || string.IsNullOrWhiteSpace(bodyItem.FormId))
                bodyItem.FormId = code;
            else
                code = bodyItem.FormId;

            var entity = await this.Find(code);

            if (isIncrement && entity != null)
            {
                CurrentNextNumber = entity.NextNum;
                entity.IncrementNumber();
                FormNumber.StopNumbering(this.CompId, entity.FormId, entity.NextNum);
                return entity;
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Form Number '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }
        #endregion Helper Methods

        #region Event Handlers
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<FormNumber> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as FormNumber);
        }
        #endregion Event Handlers

        #region Properties
        protected int CurrentNextNumber { get; set; }
        protected FormNumberProvider Provider { get; } = new FormNumberProvider();

        protected SortedDictionary<string, Action<FormNumber>> PropertyDictionary { get; } = new SortedDictionary<string, Action<FormNumber>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "c691df07-8b04-4677-9ec1-65d8d9733f15";
        #endregion
    }
}
