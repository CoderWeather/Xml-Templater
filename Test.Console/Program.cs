// var input = "    {% for product in products %}  "; // length = 36
// var result = ParseRegex.ExtractObjectNameAndCollectionName(
//     input,
//     out var lineOffset,
//     out var obj,
//     out var col
// );
// Console.WriteLine($"result: {result}");
// Console.WriteLine($"lineOffset: {lineOffset}");
// Console.WriteLine($"obj: {obj}");
// Console.WriteLine($"col: {col}");

using System.Buffers;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Templater;
using Templater.Core;
using Test.Console;

// const string template = """
//     <ul id="products">
//         {% for product in products %}
//         <li>
//             <h2>{{product.name}}</h2>
//             Only {{product.price | price }}
//             {{product.description | paragraph }}
//         </li>
//         {% endfor %}
//     </ul>
//     """;
// var json = """
//     {
//       "products": [
//         {
//           "name": "Apple",
//           "price": 329,
//           "description": "flat-out fun"
//         },
//         {
//           "name": "Orange",
//           "price": 25,
//           "description": "colorful"
//         },
//         {
//           "name": "Banana",
//           "price": 99,
//           "description": "peel it"
//         }
//       ]
//     }
//     """;

// var r1 = ParseHelper.ValidateTemplate(template);
// _ = r1;
// var b = new Bench() { N = 1000 };
// b.Setup();
// var r = b.ValidateTemplate();
// Console.WriteLine($"result: {r}");
BenchmarkRunner.Run<Bench>();

// Console.WriteLine();
