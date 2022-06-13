#region Using Directives
using TRAVERSE.Business.Inventory;
#endregion Using Directives

namespace TRAVERSE.Web.API.ProjectCosting.Models
{
    public class FindSerialItemPC
    {
        #region Fields
        private string _sItemID;

        private string _sSerialNum;

        private string _sLocId;

        private string _sLotNum;
        #endregion Fields

        #region Public
        public bool FindByItemIdLocId(SerialItem serial)
        {
            return string.Compare(this._sItemID, serial.ItemId, true) == 0 && string.Compare(this._sLocId, serial.LocId, true) == 0;
        }

        public bool FindByItemIdLocIdLotNum(SerialItem serial)
        {
            return string.Compare(this._sItemID, serial.ItemId, true) == 0 && string.Compare(this._sLocId, serial.LocId, true) == 0 && string.Compare(this._sLotNum, serial.LotNum, true) == 0;
        }

        public bool FindBySerialNumItemId(SerialItem serial)
        {
            return string.Compare(this._sSerialNum, serial.SerNum, true) == 0 && string.Compare(this._sItemID, serial.ItemId, true) == 0;
        }

        public bool FindBySerialNumItemIdLocId(SerialItem serial)
        {
            return string.Compare(this._sSerialNum, serial.SerNum, true) == 0 && string.Compare(this._sItemID, serial.ItemId, true) == 0 && string.Compare(this._sLocId, serial.LocId, true) == 0;
        }
        #endregion Public

        #region Properties
        public string ItemID
        {
            set
            {
                this._sItemID = value;
            }
        }

        public string SerialNum
        {
            set
            {
                this._sSerialNum = value;
            }
        }

        public string LocId
        {
            set
            {
                this._sLocId = value;
            }
        }

        public string LotNum
        {
            set
            {
                this._sLotNum = value;
            }
        }
        #endregion Properties
    }
}
