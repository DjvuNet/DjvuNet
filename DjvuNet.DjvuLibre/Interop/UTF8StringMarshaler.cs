//
// Code based on implementation presented in CodeProject article
// https://www.codeproject.com/Articles/138614/Advanced-Topics-in-PInvoke-String-Marshaling
// by David Jeske
// Copyright David Jeske 2010
// License  The Code Project Open License (CPOL)
// http://www.codeproject.com/info/cpol10.aspx
//
// Code modified by Jacek Błaszczyński Copyright 2017
// Dual licensed MIT and CPOL license.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.DjvuLibre
{

    /// <summary>
    /// Custom UTF8NativeString marshaller allows exposing native UTF8 handling
    /// interfaces to managed code and vice versa.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | 
        AttributeTargets.ReturnValue)]
    public class UTF8StringMarshaler : Attribute, ICustomMarshaler
    {
        private static UTF8StringMarshaler _Instance;

        public IntPtr MarshalManagedToNative(object ManagedObj)
        {
            if (ManagedObj == null)
                return IntPtr.Zero;

            String obj = ManagedObj as String;
            if (obj == null)
                throw new MarshalDirectiveException(
                       "UTF8StringMarshaler must be used on a string.");

            // not null terminated
            byte[] buffer = Encoding.UTF8.GetBytes(obj);
            IntPtr nativeUTF8Str = Marshal.AllocHGlobal(buffer.Length + 1);
            Marshal.Copy(buffer, 0, nativeUTF8Str, buffer.Length);

            // write the terminating null
            Marshal.WriteByte(nativeUTF8Str, buffer.Length, 0);
            return nativeUTF8Str;
        }

        public unsafe object MarshalNativeToManaged(IntPtr pNativeData)
        {
            byte* walk = (byte*)pNativeData;

            // find the end of the string
            while (*walk != 0)
                walk++;

            int length = (int)(walk - (byte*)pNativeData);

            // should not be null terminated
            byte[] buffer = new byte[length];
            // skip the trailing null
            Marshal.Copy(pNativeData, buffer, 0, length);
            string data = Encoding.UTF8.GetString(buffer);
            return data;
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            Marshal.FreeHGlobal(pNativeData);
        }

        public void CleanUpManagedData(object ManagedObj)
        {
        }

        public int GetNativeDataSize()
        {
            return -1;
        }

        public static ICustomMarshaler GetInstance(string cookie)
        {
            // First most probable path
            if (_Instance != null)
                return _Instance;
            else 
                return _Instance = new UTF8StringMarshaler();
            
        }
    }
}
