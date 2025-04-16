using System;
using System.Collections.Generic;
using System.Text;

namespace AIaaS.Cache
{
    //這以後要拿掉，太多種類的Cache，用ICacheManager替換CacheObject
    public class CacheObject<T>
    {
        private T _Value;
        private DateTime _UpdatedTime = DateTime.MinValue;

        public CacheObject(T value)
        {
            _Value = value;
            _UpdatedTime = DateTime.UtcNow;
        }

        public bool IsExpredHours(int hour = 1)
        {
            if (DateTime.UtcNow < _UpdatedTime.AddHours(hour))
                return false;
            return true;
        }

        public T Value
        {
            get
            {
                return _Value;
            }
            set
            {
                _Value = value;
                _UpdatedTime = DateTime.UtcNow;
            }
        }
    }
}
