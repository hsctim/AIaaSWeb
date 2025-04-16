using Abp.Configuration;
using Abp.Dependency;
using Abp.Runtime.Caching;
using Abp.Threading;
using Abp.Timing;
using AIaaS.Cache;
using AIaaS.Configuration;
using AIaaS.Helpers;
using AIaaS.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIaaS.Nlp.External
{
    public class ExternalCustomData : ITransientDependency
    {
        //private readonly NlpCacheManagerHelper _nlpLRUCacheHelper;
        private readonly ICacheManager _cacheManager;
        private readonly ISettingManager _settingManager;
        private static CacheObject<decimal> _cacheForeignExchangeRateUSD_NTD;

        private readonly string _foreignExchangeUSD_NTD_URL = "https://openapi.taifex.com.tw/v1/DailyForeignExchangeRates";

        private readonly string _weatherAPIAuthorization = "CWB-5516DE7E-E268-436C-A79D-6E4369197460";
        //private static readonly object _lock = new object();

        public ExternalCustomData(
            ICacheManager cacheManager,
            ISettingManager settingManager
            )
        {
            _cacheManager = cacheManager;
            _settingManager = settingManager;
        }


        public async Task<string> GetCustomDataAsync(string jsonData)
        {
            dynamic j = JsonConvert.DeserializeObject(jsonData);

            string intent = j.Intent.ToString().ToUpper();

            switch (intent)
            {
                case "WEATHER.ZHTW":
                    string weatherReport =
                        (string)_cacheManager.Get_ExternalData(jsonData)
                        ??
                        (string)_cacheManager.Set_ExternalData(jsonData, (string)(await GetWeatherZHTWAsync(j)));

                    return weatherReport;
                case "TIME.NOW.ZHTW":
                    return GetTimeNowZHTW();
                case "DATE.NOW.ZHTW":
                    return GetDateNowZHTW();
                default:
                    break;
            }

            return null;
        }

        public async Task<string> GetCustomDataAsync2(string strData)
        {
            // replace ${"Intent":"Time.Now.ZHTW"} to return GetTimeNowZHTW();
            strData = strData.Replace("${\"Intent\":\"Time.Now.ZHTW\"}", GetTimeNowZHTW());

            //DATE.NOW.ZHTW
            strData = strData.Replace("${\"Intent\":\"Date.Now.ZHTW\"}", GetDateNowZHTW());

            // ${\"Intent\":\"Weather.ZHTW\",\"Day\":\"2\",\"LocationName\":\"臺北市\",\"LocationId\":\"F-D0047-061\"}
            // replace ${"Intent":"Weather.ZHTW","Day":"2","LocationName":"臺北市","LocationId":"F-D0047-061"} to return GetWeatherZHTWAsync();

            return strData;
        }


        public string GetDateNowZHTW()
        {
            DateTime dt = Clock.Now.AddHours(8);
            String DayOfWeek = "";
            switch (dt.DayOfWeek)
            {
                case System.DayOfWeek.Sunday:
                    DayOfWeek = "日";
                    break;
                case System.DayOfWeek.Monday:
                    DayOfWeek = "一";
                    break;
                case System.DayOfWeek.Tuesday:
                    DayOfWeek = "二";
                    break;
                case System.DayOfWeek.Wednesday:
                    DayOfWeek = "三";
                    break;
                case System.DayOfWeek.Thursday:
                    DayOfWeek = "四";
                    break;
                case System.DayOfWeek.Friday:
                    DayOfWeek = "五";
                    break;
                case System.DayOfWeek.Saturday:
                    DayOfWeek = "六";
                    break;
                default:
                    break;
            }

            return dt.Month.ToString() + "月" + dt.Day.ToString() + "日，星期" + DayOfWeek;
        }

        public string GetTimeNowZHTW()
        {
            DateTime dt = Clock.Now.AddHours(8);
            if (dt.Hour > 12)
                dt.AddHours(-12);

            return dt.Hour.ToString() + "點" + dt.Minute.ToString() + "分";
        }


        private async Task<string> GetWeatherZHTWAsync(dynamic j)
        {
            string locationName = (j.LocationName != null ? j.LocationName : "臺北市");

            string url = $"https://opendata.cwb.gov.tw/api/v1/rest/datastore/F-D0047-091?Authorization=CWB-5516DE7E-E268-436C-A79D-6E4369197460&format=JSON&locationName={locationName}&elementName=WeatherDescription";


            int day = (j.Day != null ? Convert.ToInt32(j.Day) : 0);

            var data = await HttpRequest.GetAsync(url, null);

            dynamic j2 = JsonConvert.DeserializeObject(data);

            if (j2.success != "true")
                return null;

            //get date of todayMorning
            string morning = DateTime.Now.AddDays(day).ToString("yyyy-MM-dd 06:00:00");
            string evening = DateTime.Now.AddDays(day).ToString("yyyy-MM-dd 18:00:00");

            string description = "";

            var weathers = j2.records.locations[0].location[0].weatherElement[0].time;
            foreach (var item in weathers)
            {
                if (item.startTime.ToString() == morning)
                    description = description + "白天氣象" + ShortWeatherSescription(item.elementValue[0].value.ToString());
                else if (item.startTime.ToString() == evening)
                    description = description + "夜間氣象" + ShortWeatherSescription(item.elementValue[0].value.ToString());
            }

            return description;
        }


        public static string ShortWeatherSescription(string description)
        {
            int index = description.LastIndexOf("風速");
            description = description.Substring(0, index);
            index = description.LastIndexOf("。");
            description = description.Substring(0, index + 1);
            return description;
        }


        public async Task<decimal> GetForeignExchangeRateUSD_NTDAsync()
        {
            if (_cacheForeignExchangeRateUSD_NTD == null)
            {
                var value = await _settingManager.GetSettingValueAsync<decimal>(AppSettings.ExternalData.GetForeignExchangeRateUSD_NTD);
            }

            if (_cacheForeignExchangeRateUSD_NTD == null || _cacheForeignExchangeRateUSD_NTD.Value == 0 || _cacheForeignExchangeRateUSD_NTD.IsExpredHours())
            {
                Dictionary<string, string> header = new Dictionary<string, string>();
                header["accept"] = "application/json";

                var data = await HttpRequest.GetAsync(_foreignExchangeUSD_NTD_URL, header);

                dynamic json = JsonConvert.DeserializeObject(data);

                string ntd = json.Last["USD/NTD"].Value;

                var newValue = Decimal.Parse(ntd);

                if (_cacheForeignExchangeRateUSD_NTD == null || _cacheForeignExchangeRateUSD_NTD.Value == 0)
                {
                    _cacheForeignExchangeRateUSD_NTD = new CacheObject<decimal>(newValue);
                    await _settingManager.ChangeSettingForApplicationAsync(AppSettings.ExternalData.GetForeignExchangeRateUSD_NTD, newValue.ToString());
                }
                else
                {
                    //如果匯率變化過大，超過10%則略過更新
                    if (Math.Abs(_cacheForeignExchangeRateUSD_NTD.Value - newValue) * 10 < newValue && _cacheForeignExchangeRateUSD_NTD.Value != newValue)
                    {
                        _cacheForeignExchangeRateUSD_NTD = new CacheObject<decimal>(newValue);
                        await _settingManager.ChangeSettingForApplicationAsync(AppSettings.ExternalData.GetForeignExchangeRateUSD_NTD, newValue.ToString());
                    }
                }
            }

            if (_cacheForeignExchangeRateUSD_NTD == null || _cacheForeignExchangeRateUSD_NTD.Value == 0)
                throw new ApplicationException("ExternalCustomData.GetForeignExchangeUSD_NTD() Error");

            return _cacheForeignExchangeRateUSD_NTD.Value;
        }
    }
}
