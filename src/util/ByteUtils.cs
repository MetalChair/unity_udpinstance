using System;

public static class ByteUtils
{
    public static byte[] GetBytes<T>(T value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        switch (Type.GetTypeCode(typeof(T)))
        {
            case TypeCode.Byte:
                return new byte[] { (byte)(object)value };
            case TypeCode.Boolean:
                return BitConverter.GetBytes((bool)(object)value);
            case TypeCode.Char:
                return BitConverter.GetBytes((char)(object)value);
            case TypeCode.Double:
                return BitConverter.GetBytes((double)(object)value);
            case TypeCode.Int16:
                return BitConverter.GetBytes((short)(object)value);
            case TypeCode.Int32:
                return BitConverter.GetBytes((int)(object)value);
            case TypeCode.Int64:
                return BitConverter.GetBytes((long)(object)value);
            case TypeCode.Single:
                return BitConverter.GetBytes((float)(object)value);
            default:
                throw new ArgumentException("Unsupported type", nameof(value));
        }
    }
}