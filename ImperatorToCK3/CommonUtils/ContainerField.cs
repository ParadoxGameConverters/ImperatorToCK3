using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;

namespace ImperatorToCK3.CommonUtils {
    public struct ContainerFieldStruct {
        string fieldName;
        string setter;
        List<string> initialValue;
    }

    class DescendingComparer<T> : IComparer<T> where T : IComparable<T> {
        public int Compare(T x, T y) {
            return y.CompareTo(x);
        }
    }
    public class ContainerField {
        public ContainerField(List<string> initialValue) {
        }
        List<string> GetValue(Date date) {

        }
        public SortedDictionary<Date, List<string>> ValueHistory { get; set; } = new(new DescendingComparer<Date>());
    }
}
