using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace SDILReader
{
    public static class Globals
    {
        public static OpCode[] multiByteOpCodes;
        public static OpCode[] singleByteOpCodes;


        /// <summary>
        /// Loads the OpCodes for later use.
        /// </summary>
        public static void LoadOpCodes()
        {
            singleByteOpCodes = new OpCode[0x100];
            multiByteOpCodes = new OpCode[0x100];
            FieldInfo[] infoArray1 = typeof(OpCodes).GetFields();
            for (int num1 = 0; num1 < infoArray1.Length; num1++)
            {
                FieldInfo info1 = infoArray1[num1];
                if (info1.FieldType == typeof(OpCode))
                {
                    OpCode code1 = (OpCode)info1.GetValue(null);
					//try {
						unchecked {
							ushort num2 = (ushort)code1.Value;
							if (num2 < 0x100) {
								singleByteOpCodes[(int)num2] = code1;
							}
							else {
								if ((num2 & 0xff00) != 0xfe00) {
									throw new Exception("Invalid OpCode.");
								}
								multiByteOpCodes[num2 & 0xff] = code1;
							}
						}
					//}
					//catch (Exception e) {
					//    //throw e;
					//}
                }
            }
        }


        /// <summary>
        /// Retrieve the friendly name of a type
        /// </summary>
        /// <param name="typeName">
        /// The complete name to the type
        /// </param>
        /// <returns>
        /// The simplified name of the type (i.e. "int" instead f System.Int32)
        /// </returns>
        public static string ProcessSpecialTypes(string typeName)
        {
            string result = typeName;
            switch (typeName)
            {
                case "System.string":
                case "System.String":
                case "String":
                    result = "string"; break;
                case "System.Int32":
                case "Int":
                case "Int32":
                    result = "int"; break;
            }
            return result;
        }
    }
}
