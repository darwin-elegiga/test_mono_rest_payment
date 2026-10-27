using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common
{
    public static class DriverFileHelper
    {
        public static string PadRightOrTruncate(this string value, int paddingLength, char paddingChar = ' ')
        {
            if(paddingLength > value.Length)
            {
                value = value.PadRight(paddingLength, paddingChar);
            }
            else if(paddingLength < value.Length)
            {
                value = value.Substring(0, paddingLength);
            }

            return value;
        }

        public static string ZeroPad(this int value, int paddingLength)
        {
            var textValue = value.ToString();

            if(paddingLength > textValue.Length)
            {
                textValue = textValue.PadLeft(paddingLength, '0');
            }
            else if(paddingLength < textValue.Length)
            {
                textValue = textValue.Substring(0, paddingLength);
            }

            return textValue;
        }

        public static string ZeroPad(this long value, int paddingLength)
        {
            var textValue = value.ToString();

            if(paddingLength > textValue.Length)
            {
                textValue = textValue.PadLeft(paddingLength, '0');
            }
            else if(paddingLength < textValue.Length)
            {
                textValue = textValue.Substring(0, paddingLength);
            }

            return textValue;
        }

        public static void WriteDriverFileValue(this TextWriter writer, string value, int paddingLength, char paddingChar = ' ')
        {
            writer.Write(PadRightOrTruncate(value, paddingLength, paddingChar) + "|");
        }
    }
}
