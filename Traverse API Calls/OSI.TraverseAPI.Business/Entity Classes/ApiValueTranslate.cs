using System;
using System.ComponentModel;

namespace OSI.TraverseApi.Business
{
    [Serializable]
    public class ApiValueTranslate : INotifyPropertyChanged
    {
        #region Constructors
        public ApiValueTranslate()
        { }
        #endregion Constructors

        #region Methods
        protected void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion Methods

        #region Properties
        [Bindable(true), Description]
        public string Key
        {
            get => _key;
            set
            {
                _key = value;
                RaisePropertyChanged("Key");
            }
        }

        [Bindable(true), Description]
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                RaisePropertyChanged("Value");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion Properties

        #region Fields
        private string _key;
        private string _value;
        #endregion Fields
    }
}
