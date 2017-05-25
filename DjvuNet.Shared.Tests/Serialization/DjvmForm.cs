using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DjvuNet.Serialization
{
    public class DjvmForm : ElementBase
    {
        private Dirm _Dirm;
        private Navm _Navm;

        public Dirm Dirm
        {
            get
            {
                if (_Dirm != null)
                    return _Dirm;
                else
                {
                    _Dirm = (Dirm) Children[0];
                    return _Dirm;
                }
            }
            set
            {
                _Dirm = value;
            }
        }

        public Navm Navm
        {
            get
            {
                if (_Navm != null)
                    return _Navm;
                else
                {
                    _Navm = (Navm)Children.Where(x => x.ID == "NAVM").FirstOrDefault();
                    return _Navm;
                }
            }
            set
            {
                _Navm = value;
            }
        }
    }
}
