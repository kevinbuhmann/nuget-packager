using System;
using System.Runtime.Serialization;
using Vstack.Extensions;

namespace NugetPackager
{
    [Serializable]
    public class CommandLineException : Exception
    {
        public CommandLineException(CommandLineResult result)
        {
            this.Result = result;
        }

        protected CommandLineException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            info.ValidateNotNullParameter(nameof(info));

            this.Result = info.GetValue(nameof(this.Result), typeof(CommandLineResult)) as CommandLineResult;
        }

        public CommandLineResult Result { get; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.ValidateNotNullParameter(nameof(info));

            info.AddValue(nameof(this.Result), this.Result, typeof(CommandLineResult));

            base.GetObjectData(info, context);
        }
    }
}
