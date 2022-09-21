using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.Data.Utilities;

namespace netCDFLibrary.Data
{
    public struct DataScale
    {
        private static readonly string[] defaultScaleFactorKeys = new string[] { "scale_factor", "ScaleFactor" };
        private static readonly string[] defaultAddOffsetKeys = new string[] { "add_offset", "AddOffset" };
        private static readonly string[] defaultMissingValueKeys = new string[] { "missing_value", "MissingValue" };
        private static readonly string[] defaultFillValueKeys = new string[] { "_fill_value", "_FillValue", "fill_value", "FillValue" };

        public readonly double ScaleFactor = 1.0;
        public readonly double AddOffset = 0;
        public readonly double FillValue = double.NaN;
        public readonly double MissingValue = double.NaN;

        public readonly string? Name = "";
        public readonly string? LongName = "";
        public readonly string? StandardName = "";
        public readonly string? Units = "";
        internal DataScale(Variable variable) : this(variable, defaultScaleFactorKeys, defaultAddOffsetKeys, defaultMissingValueKeys, defaultFillValueKeys) { }
        internal DataScale(Variable variable, string[] ScaleFactorKeys, string[] AddOffsetKeys, string[] MissingValueKeys, string[] FillValueKeys)
        {
            this.Name = variable.Metadata.GetDisplayName();
            this.Units = variable.Metadata.GetUnits();
            if (variable.Metadata.ContainsKey("long_name"))
            {
                this.LongName = Convert.ToString(variable.Metadata["long_name"]);
            }
            if (variable.Metadata.ContainsKey("standard_name"))
            {
                this.StandardName = Convert.ToString(variable.Metadata["standard_name"]);
            }
            foreach (string scaleFactor in ScaleFactorKeys)
            {
                if (variable.Metadata.ContainsKey(scaleFactor))
                {
                    this.ScaleFactor = Convert.ToDouble(variable.Metadata[scaleFactor]);
                }
            }
            foreach (string addOffset in AddOffsetKeys)
            {
                if (variable.Metadata.ContainsKey(addOffset))
                {
                    this.AddOffset = Convert.ToDouble(variable.Metadata[addOffset]);
                }
            }
            foreach (string fillValue in FillValueKeys)
            {
                if (variable.Metadata.ContainsKey(fillValue))
                {
                    this.FillValue = Convert.ToDouble(variable.Metadata[fillValue]);
                }
            }
            foreach (string missingValue in MissingValueKeys)
            {
                if (variable.Metadata.ContainsKey(missingValue))
                {
                    this.MissingValue = Convert.ToDouble(variable.Metadata[missingValue]);
                }
            }
        }
        internal double Transform(object? value)
        {
            if (value == null)
            {
                return this.MissingValue;
            }
            if(Convert.ToDouble(value) == this.MissingValue)
            {
                return this.MissingValue;
            }
            switch (value)
            {
                case short sValue: return (sValue * this.ScaleFactor) + this.AddOffset;
                default: return (double)value;
            }
        }
    }
}
