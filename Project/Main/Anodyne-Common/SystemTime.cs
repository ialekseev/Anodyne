﻿// Copyright 2011-2012 Anodyne.
//   
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
//  
//      http://www.apache.org/licenses/LICENSE-2.0 
//  
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.

namespace Kostassoid.Anodyne.Common
{
    using System;
    using System.ComponentModel;

    public static class SystemTime
    {
        private static readonly ITimeController Controller = new InternalTimeController();

        public static DateTime Now
        {
            get { return Controller.CurrentDateTime; }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static ITimeController TimeController
        {
            get { return Controller; }
        }

        #region Nested type: InternalTimeController

        private class InternalTimeController : ITimeController
        {
            private static readonly Func<DateTime> DefaultSystemDateFunc = () => DateTime.Now;

            private Func<DateTime> _dateFunc = DefaultSystemDateFunc;

            #region ITimeController Members

            public DateTime CurrentDateTime
            {
                get { return _dateFunc().ToUniversalTime(); }
            }

            public void Customize(Func<DateTime> func)
            {
                _dateFunc = func;
            }

            public void SetDate(DateTime date)
            {
                var whnStd = DefaultSystemDateFunc();
                Func<DateTime> func = () => date + (DefaultSystemDateFunc() - whnStd);
                _dateFunc = func;
            }

            public void SetFrozenDate(DateTime date)
            {
                _dateFunc = () => date;
            }

            public void Reset()
            {
                _dateFunc = DefaultSystemDateFunc;
            }

            #endregion
        }

        #endregion
    }

    public interface ITimeController
    {
        DateTime CurrentDateTime { get; }
        void Customize(Func<DateTime> dateFunc);
        void SetDate(DateTime date);
        void SetFrozenDate(DateTime date);
        void Reset();
    }
}