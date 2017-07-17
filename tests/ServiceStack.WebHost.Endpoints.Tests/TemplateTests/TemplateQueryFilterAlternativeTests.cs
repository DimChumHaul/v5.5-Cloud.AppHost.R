using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateQueryFilterAlternativeTests
    {
        private static TemplateContext CreateContext(Dictionary<string, object> optionalArgs = null)
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["numbers"] = new[] { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 },
                    ["products"] = TemplateQueryData.Products,
                    ["customers"] = TemplateQueryData.Customers,
                }
            };
            optionalArgs.Each((key, val) => context.Args[key] = val);
            return context.Init();
        }

        [Test]
        public void linq1_original()
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateTemplate(@"
Numbers < 5:
{{ numbers | where('it < 5') | select('{{ it }}\n') }}").NormalizeNewLines(), 
                
                Is.EqualTo(@"
Numbers < 5:
4
1
3
2
0
".NormalizeNewLines()));
        }

        [Test]
        public void linq2_original()
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateTemplate(@"
Sold out products:
{{ products 
   | where('it.UnitsInStock = 0') 
   | select('{{ it.productName | raw }} is sold out!\n')
}}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Sold out products:
Chef Anton's Gumbo Mix is sold out!
Alice Mutton is sold out!
Thüringer Rostbratwurst is sold out!
Gorgonzola Telino is sold out!
Perth Pasties is sold out!
".NormalizeNewLines()));
        }

        [Test]
        public void linq2_original_with_custom_item_binding()
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateTemplate(@"
Sold out products:
{{ products 
   | where('product.UnitsInStock = 0', { it: 'product' }) 
   | select('{{ product.productName | raw }} is sold out!\n', { it: 'product' })
}}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Sold out products:
Chef Anton's Gumbo Mix is sold out!
Alice Mutton is sold out!
Thüringer Rostbratwurst is sold out!
Gorgonzola Telino is sold out!
Perth Pasties is sold out!
".NormalizeNewLines()));
        }

        [Test]
        public void linq4_selectPartial()
        {
            var context = CreateContext(new Dictionary<string, object>
            {
                {TemplateConstants.DefaultDateFormat, "yyyy/MM/dd"}
            });
 
            context.VirtualFiles.WriteFile("page.html", @"{{ 
  customers 
  | where: it.Region = 'WA' 
  | assignTo: waCustomers 
}}
Customers from Washington and their orders:
{{ waCustomers | selectPartial('customer') }}");

            context.VirtualFiles.WriteFile("customer.html", @"Customer {{ it.CustomerId }} {{ it.CompanyName | raw }}
{{ it.Orders | select(""  Order {{ it.OrderId }}: {{ it.OrderDate | dateFormat | newLine }}"") }}");
            
            Assert.That(new PageResult(context.GetPage("page")).Result.NormalizeNewLines(),
                Does.StartWith(@"
Customers from Washington and their orders:
Customer LAZYK Lazy K Kountry Store
  Order 10482: 1997/03/21
  Order 10545: 1997/05/22
Customer TRAIH Trail's Head Gourmet Provisioners
  Order 10574: 1997/06/19
  Order 10577: 1997/06/23
  Order 10822: 1998/01/08
".NormalizeNewLines()));
        }

        [Test]
        public void linq4_selectPartial_nested()
        {
            var context = CreateContext(new Dictionary<string, object>
            {
                {TemplateConstants.DefaultDateFormat, "yyyy/MM/dd"}
            });
 
            context.VirtualFiles.WriteFile("page.html", @"{{ 
  customers 
  | where: it.Region = 'WA' 
  | assignTo: waCustomers 
}}
Customers from Washington and their orders:
{{ waCustomers | selectPartial: customer }}");
            
            context.VirtualFiles.WriteFile("customer.html", 
                @"Customer {{ it.CustomerId }} {{ it.CompanyName | raw }}
{{ it.Orders | selectPartial: order }}");
            
            context.VirtualFiles.WriteFile("order.html", @"  Order {{ it.OrderId }}: {{ it.OrderDate | dateFormat}}
");
            
            Assert.That(new PageResult(context.GetPage("page")).Result.NormalizeNewLines(),
                Does.StartWith(@"
Customers from Washington and their orders:
Customer LAZYK Lazy K Kountry Store
  Order 10482: 1997/03/21
  Order 10545: 1997/05/22
Customer TRAIH Trail's Head Gourmet Provisioners
  Order 10574: 1997/06/19
  Order 10577: 1997/06/23
  Order 10822: 1998/01/08
".NormalizeNewLines()));
        }

        [Test]
        public void linq4_selectPartial_nested_with_custom_item_binding()
        {
            var context = CreateContext(new Dictionary<string, object>
            {
                {TemplateConstants.DefaultDateFormat, "yyyy/MM/dd"}
            });
 
            context.VirtualFiles.WriteFile("page.html", @"{{ 
  customers 
  | where: it.Region = 'WA' 
  | assignTo: waCustomers 
}}
Customers from Washington and their orders:
{{ waCustomers | selectPartial: customer }}");
            
            context.VirtualFiles.WriteFile("customer.html", 
                @"
<!--
it: cust
-->

Customer {{ cust.CustomerId }} {{ cust.CompanyName | raw }}
{{ cust.Orders | selectPartial('order', { it: 'order' })  }}");
            
            context.VirtualFiles.WriteFile("order.html", "  Order {{ order.OrderId }}: {{ order.OrderDate | dateFormat}}\n");
            
            Assert.That(new PageResult(context.GetPage("page")).Result.NormalizeNewLines(),
                Does.StartWith(@"
Customers from Washington and their orders:
Customer LAZYK Lazy K Kountry Store
  Order 10482: 1997/03/21
  Order 10545: 1997/05/22
Customer TRAIH Trail's Head Gourmet Provisioners
  Order 10574: 1997/06/19
  Order 10577: 1997/06/23
  Order 10822: 1998/01/08
".NormalizeNewLines()));
        }
        
    }
}