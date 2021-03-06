﻿using Common.Dto;
using Common.Tools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using static Common.Dto.ScheduleDto;

namespace Common.Converter
{
    public class JsonDataToTimerConverter
    {
        private const string TAG = "JsonDataToTimerConverter";
        private static string _searchParameter = "{\"Data\":";

        private static JsonDataToTimerConverter _instance = null;
        private static readonly object _padlock = new object();

        JsonDataToTimerConverter()
        {
            // Empty constructor, nothing needed here
        }

        public static JsonDataToTimerConverter Instance
        {
            get
            {
                lock (_padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new JsonDataToTimerConverter();
                    }

                    return _instance;
                }
            }
        }

        public IList<TimerDto> GetList(string[] stringArray, IList<WirelessSocketDto> socketList, IList<WirelessSwitchDto> switchList)
        {
            if (StringHelper.StringsAreEqual(stringArray))
            {
                return parseStringToList(stringArray[0], socketList, switchList);
            }
            else
            {
                string usedEntry = StringHelper.SelectString(stringArray, _searchParameter);
                return parseStringToList(usedEntry, socketList, switchList);
            }
        }

        public IList<TimerDto> GetList(string jsonString, IList<WirelessSocketDto> socketList, IList<WirelessSwitchDto> switchList)
        {
            return parseStringToList(jsonString, socketList, switchList);
        }

        private IList<TimerDto> parseStringToList(string value, IList<WirelessSocketDto> socketList, IList<WirelessSwitchDto> switchList)
        {
            if (!value.Contains("Error"))
            {
                IList<TimerDto> timerList = new List<TimerDto>();

                try
                {
                    JObject jsonObject = JObject.Parse(value);
                    JToken jsonObjectData = jsonObject.GetValue("Data");

                    foreach (JToken child in jsonObjectData.Children())
                    {
                        JToken timerJsonData = child["Schedule"];

                        bool isTimer = timerJsonData["IsTimer"].ToString() == "1";
                        if (!isTimer)
                        {
                            continue;
                        }

                        int id = int.Parse(timerJsonData["Id"].ToString());

                        string name = timerJsonData["Name"].ToString();

                        string socketName = timerJsonData["Socket"].ToString();
                        WirelessSocketDto wirelessSocket = null;
                        for (int index = 0; index < socketList.Count; index++)
                        {
                            if (socketList[index].Name.Contains(socketName))
                            {
                                wirelessSocket = socketList[index];
                                break;
                            }
                        }

                        string gpioName = timerJsonData["Gpio"].ToString();
                        // TODO Gpios currently not supported in LucaHome WPF

                        string switchName = timerJsonData["Switch"].ToString();
                        WirelessSwitchDto wirelessSwitch = null;
                        for (int index = 0; index < switchList.Count; index++)
                        {
                            if (switchList[index].Name.Contains(switchName))
                            {
                                wirelessSwitch = switchList[index];
                                break;
                            }
                        }

                        int weekday = int.Parse(timerJsonData["Weekday"].ToString());

                        int hour = int.Parse(timerJsonData["Hour"].ToString());
                        int minute = int.Parse(timerJsonData["Minute"].ToString());

                        DateTime time = new DateTime(1970, 1, 1, hour, minute, 0);

                        int scheduleDayOfWeekInteger = (int)time.DayOfWeek;
                        int difference = scheduleDayOfWeekInteger - weekday;
                        time = time.AddDays(difference);

                        WirelessAction wirelessAction = timerJsonData["Action"].ToString() == "1" ? WirelessAction.Activate : WirelessAction.Deactivate;
                        bool isActive = timerJsonData["IsActive"].ToString() == "1";

                        TimerDto newTimer = new TimerDto(id, name, wirelessSocket, wirelessSwitch, time, wirelessAction, isActive);
                        timerList.Add(newTimer);
                    }
                }
                catch (Exception exception)
                {
                    Logger.Instance.Error(TAG, exception.Message);
                }

                return timerList;
            }

            Logger.Instance.Error(TAG, string.Format("{0} has an error!", value));

            return new List<TimerDto>();
        }
    }
}
