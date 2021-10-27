using System;
using System.Collections.Generic;
using System.Text;

namespace ef_bug
{
    internal class ProjectedOrder
    {
        public Guid Id { get; set; }
        public decimal Sum { get; set; }
        public decimal SpecialSum { get; set; }
    }
}
