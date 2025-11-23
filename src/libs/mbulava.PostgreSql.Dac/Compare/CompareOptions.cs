using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Compare
{
    public class CompareOptions
    {
        // General toggles
        public bool CompareOwners { get; set; } = true;
        public bool ComparePrivileges { get; set; } = true;

        // Sequence-specific toggles
        public bool CompareSequenceStart { get; set; } = false;   // reseeding disabled by default
        public bool CompareSequenceIncrement { get; set; } = true;
        public bool CompareSequenceMinValue { get; set; } = true;
        public bool CompareSequenceMaxValue { get; set; } = true;
        public bool CompareSequenceCache { get; set; } = true;
        public bool CompareSequenceCycle { get; set; } = true;

        // Table-specific toggles
        public bool CompareColumns { get; set; } = true;
        public bool CompareConstraints { get; set; } = true;
        public bool CompareIndexes { get; set; } = true;

        // Type-specific toggles
        public bool CompareEnumLabels { get; set; } = true;
        public bool CompareCompositeAttributes { get; set; } = true;
    }
}
