using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;

namespace SDILReader
{
    public class ILInstruction
    {
        #region fields
        private OpCode code;
        private object operand;
        private byte[] operandData;
        private int offset;
        #endregion

        #region Properties
        public OpCode Code
        {
            get { return code; }
            set { code = value; }
        }

        public object Operand
        {
            get { return operand; }
            set { operand = value; }
        }

        public byte[] OperandData
        {
            get { return operandData; }
            set { operandData = value; }
        }

        public int Offset
        {
            get { return offset; }
            set { offset = value; }
        }

        #endregion

        /// <summary>
        /// Returns a friendly strign representation of this instruction
        /// </summary>
        /// <returns></returns>
        public string GetCode()
        {
            string result = "";
            result += GetExpandedOffset(offset) + " : " + code;
            if (operand != null)
            {
                switch (code.OperandType)
                {
                    case OperandType.InlineField:
                        System.Reflection.FieldInfo fOperand = ((System.Reflection.FieldInfo)operand);
                        result += " " + Globals.ProcessSpecialTypes(fOperand.FieldType.ToString()) + " " +
                            Globals.ProcessSpecialTypes(fOperand.ReflectedType.ToString()) +
                            "::" + fOperand.Name + "";
                        break;
                    case OperandType.InlineMethod:
                        try
                        {
                            System.Reflection.MethodInfo mOperand = (System.Reflection.MethodInfo)operand;
                            result += " ";
                            if (!mOperand.IsStatic) result += "instance ";
                            result += Globals.ProcessSpecialTypes(mOperand.ReturnType.ToString()) +
                                " " + Globals.ProcessSpecialTypes(mOperand.ReflectedType.ToString()) +
                                "::" + mOperand.Name + "()";
                        }
                        catch
                        {
                            try
                            {
                                System.Reflection.ConstructorInfo mOperand = (System.Reflection.ConstructorInfo)operand;
                                result += " ";
                                if (!mOperand.IsStatic) result += "instance ";
                                result += "void " +
                                    Globals.ProcessSpecialTypes(mOperand.ReflectedType.ToString()) +
                                    "::" + mOperand.Name + "()";
                            }
                            catch
                            {
                            }
                        }
                        break;
                    case OperandType.ShortInlineBrTarget:
                        result += " " + GetExpandedOffset((int)operand);
                        break;
                    case OperandType.InlineType:
                        result += " " + Globals.ProcessSpecialTypes(operand.ToString());
                        break;
                    case OperandType.InlineString:
                        if (operand.ToString() == "\r\n") result += " \"\\r\\n\"";
                        else result += " \"" + operand.ToString() + "\"";
                        break;
                    default: result += "not supported"; break;
                }
            }
            return result;

        }

        /// <summary>
        /// Add enough zeros to a number as to be represented on 4 characters
        /// </summary>
        /// <param name="offset">
        /// The number that must be represented on 4 characters
        /// </param>
        /// <returns>
        /// </returns>
        private string GetExpandedOffset(int offset)
        {
            string result = offset.ToString();
            for (int i = 0; result.Length < 4; i++)
            {
                result = "0" + result;
            }
            return result;
        }

        /// <summary>
        /// Normal constructor.
        /// </summary>
        public ILInstruction()
        {

        }
    }
}
