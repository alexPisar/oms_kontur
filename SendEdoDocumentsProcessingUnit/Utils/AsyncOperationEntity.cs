using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdiProcessingUnit.Infrastructure;

namespace SendEdoDocumentsProcessingUnit.Utils
{
    public class AsyncOperationEntity<TEntity> where TEntity : new()
    {
        private TEntity _entity;
        private Exception _exception;
        private string _description;

        public AsyncOperationEntity()
        {
            _exception = null;
        }

        public TEntity Entity
        {
            get {
                if (Exception != null)
                    return default(TEntity);

                return _entity;
            }
            set {
                _entity = value;
            }
        }

        public Exception Exception
        {
            get {
                return _exception;
            }
        }

        public string Description
        {
            get {
                return _description;
            }

            set {
                _description = value;
            }
        }

        public void SetException(Exception exception, string description = null)
        {
            _exception = exception;

            if (!string.IsNullOrEmpty(description))
                _description = description;
        }
    }
}
