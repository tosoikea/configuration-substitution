﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;

namespace adesso.BusinessProcesses.ConfigurationSubstitution
{
    public class NamingData
    {
        /*
         https://social.msdn.microsoft.com/Forums/en-US/8275f918-5eeb-473d-acfa-c3e6dadf7f3f/-c-naming-convention-for-the-method?forum=csharplanguage
         */

        // PROPERTIES
        [JsonProperty]
        public Dictionary<String, String> BaseValues;

        // METHODS
        public static explicit operator Dictionary<string, string>(NamingData naming) => (naming != null) ? naming.BaseValues : new Dictionary<string, string>();

        public void AssureComparer()
        {
            if (BaseValues.Comparer.Equals(StringComparer.OrdinalIgnoreCase))
                return;

            Dictionary<String, String> correctComparer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var baseEnum = BaseValues.GetEnumerator(); baseEnum.MoveNext();)
                correctComparer.Add(baseEnum.Current.Key, baseEnum.Current.Value);

            BaseValues = correctComparer;
        }

        public bool ContainsKey(String key)
        {
            AssureComparer();
            return BaseValues.ContainsKey(key);
        }

        public String Get(String key)
        {
            AssureComparer();
            return BaseValues[key];
        }

        public bool TryGet(string key, ref string value)
        {
            bool isContained = ContainsKey(key);
            if (!isContained)
                return isContained;

            if (value != null)
                value = BaseValues[key];

            return isContained;
        }

        #region Insert
        public void Insert(Hashtable values)
        {
            if (values == null || values.Count == 0)
                return;

            for (var valueEnum = values.GetEnumerator(); valueEnum.MoveNext();)
                Add(true, valueEnum.Key.ToString(), valueEnum.Value);
        }

        public void Insert(PSObject values)
        {
            if (values == null)
                return;

            for (var memberEnum = values.Members.GetEnumerator(); memberEnum.MoveNext();)
                if (memberEnum.Current.MemberType == PSMemberTypes.Property || memberEnum.Current.MemberType == PSMemberTypes.Properties || memberEnum.Current.MemberType == PSMemberTypes.NoteProperty)
                    Add(true, memberEnum.Current.Name, memberEnum.Current.Value);
        }
        public void Insert(Dictionary<string, object> values)
        {
            if (values != null && values.Count > 0)
                for (var valueEnum = values.GetEnumerator(); valueEnum.MoveNext();)
                    Add(true, valueEnum.Current.Key.ToString(), valueEnum.Current.Value);
        }
        public void Insert(Dictionary<string, string> values)
        {
            if (values != null && values.Count > 0)
                for (var valueEnum = values.GetEnumerator(); valueEnum.MoveNext();)
                    Add(true, valueEnum.Current.Key.ToString(), valueEnum.Current.Value);
        }

        public void Insert(String key, Object value) => Add(true, key, value);
        #endregion


        #region TryAdd
        public bool TryAdd(NamingData naming)
        {
            bool isUnique = true;

            if (naming != null && naming.BaseValues.Count > 0)
                for (var valueEnum = naming.BaseValues.GetEnumerator(); valueEnum.MoveNext();)
                    isUnique = TryAdd(valueEnum.Current.Key, valueEnum.Current.Value) && isUnique;

            return isUnique;
        }

        public bool TryAdd(Hashtable values)
        {
            bool isUnique = true;

            if (values != null && values.Count > 0)
                for (var valueEnum = values.GetEnumerator(); valueEnum.MoveNext();)
                    isUnique = TryAdd(valueEnum.Key.ToString(), valueEnum.Value) && isUnique;

            return isUnique;
        }

        public bool TryAdd(PSObject values)
        {
            bool isUnique = true;

            if (values != null)
                for (var memberEnum = values.Members.GetEnumerator(); memberEnum.MoveNext();)
                    if (memberEnum.Current.MemberType == PSMemberTypes.Property || memberEnum.Current.MemberType == PSMemberTypes.Properties || memberEnum.Current.MemberType == PSMemberTypes.NoteProperty)
                        isUnique = TryAdd(memberEnum.Current.Name, memberEnum.Current.Value) && isUnique;

            return isUnique;
        }
        public bool TryAdd(Dictionary<string, object> values)
        {
            bool isUnique = true;

            if (values != null && values.Count > 0)
                for (var valueEnum = values.GetEnumerator(); valueEnum.MoveNext();)
                    isUnique = TryAdd(valueEnum.Current.Key, valueEnum.Current.Value) && isUnique;

            return isUnique;
        }
        public bool TryAdd(Dictionary<string, string> values)
        {
            bool isUnique = true;

            if (values != null && values.Count > 0)
                for (var valueEnum = values.GetEnumerator(); valueEnum.MoveNext();)
                    isUnique = TryAdd(valueEnum.Current.Key, valueEnum.Current.Value) && isUnique;

            return isUnique;
        }

        public bool TryAdd(String key, Object value) => Add(false, key, value);
        #endregion

        private void BreakupValue(ref String result, Object value)
        {
            AssureComparer();
            if (value == null)
                return;

            if (value is String)
            {
                result += value;
                return;
            }

            Type valueType = value.GetType();

            if (valueType.IsArray)
            {
                Array container = (Array)value;
                for (int aI = 0; aI < container.Length; aI++)
                    if (container.GetValue(aI) != null)
                    {
                        if (aI > 0)
                            result += ", ";
                        BreakupValue(ref result, container.GetValue(aI));
                    }
            }
            else if (typeof(IEnumerable).IsAssignableFrom(valueType))
            {
                int index = 0;
                for (var valueEnum = ((IEnumerable)value).GetEnumerator(); valueEnum.MoveNext(); index++)
                {
                    if (valueEnum.Current != null)
                    {
                        if (index > 0)
                            result += ",";
                        BreakupValue(ref result, valueEnum.Current);
                    }
                }
            }
            else
                result += value.ToString();
        }

        private bool Add(bool overwrite, String key, Object value)
        {
            bool isUnique = true;
            String finalResult = String.Empty;

            if (value == null || String.IsNullOrEmpty(key))
                return false;

            BreakupValue(ref finalResult, value);

            if (!ContainsKey(key))
            {
                BaseValues.Add(key, finalResult);
            }
            else
            {
                isUnique = false;
                if (overwrite)
                    BaseValues[key] = finalResult;
            }

            return isUnique;
        }

        override
        public String ToString()
        {
            StringBuilder jsonBuilder = new StringBuilder();

            using (var stringWriter = new StringWriter(jsonBuilder))
            {
                //very important for linux systems
                stringWriter.NewLine = "\n";

                //indent child keys (better looking)
                var serializer = JsonSerializer.Create(
                    new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented
                    });

                serializer.Serialize(stringWriter, BaseValues);
            }

            return jsonBuilder.ToString();
        }


        #region Constructors
        public NamingData() => this.BaseValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public NamingData(Dictionary<string, object> naming) : this() => TryAdd(naming);
        public NamingData(Hashtable naming) : this() => TryAdd(naming);
        public NamingData(PSObject naming) : this() => TryAdd(naming);

        public NamingData(NamingData naming) : this()
        {
            if (naming == null || naming.BaseValues == null)
                return;
            Insert((Dictionary<string, string>)naming);
            AssureComparer();
        }

        [JsonConstructor]
        public NamingData(JObject jObject) : this()
        {
            if (jObject == null || jObject.Count == 0)
                return;

            for (var jEnum = jObject.GetEnumerator(); jEnum.MoveNext();)
                TryAdd(jEnum.Current.Key, jEnum.Current.Value);
        }
        #endregion

    }
}
