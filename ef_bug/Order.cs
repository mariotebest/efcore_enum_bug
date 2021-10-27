using System;
using System.Collections.Generic;
using System.Text;

namespace ef_bug
{
    internal class Order
    {
        public Guid Id {  get; set; }    

        public virtual ICollection<OrderItem> Items { get; set; }
    }
}
