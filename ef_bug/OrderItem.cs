using System;
using System.Collections.Generic;
using System.Text;

namespace ef_bug
{
    internal class OrderItem
    {
        public Guid Id {  get; set; }
        public Guid OrderId { get; set; }
        public OrderItemType Type { get; set; }
        public decimal Price { get; set; }
    }
}
