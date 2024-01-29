using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

const string template = """
    <ul id="products">
        {% for product in products %}
        <li>
            <h2>{{product.name}}</h2>
            Only {{product.price | price }}
            {{product.description | paragraph }}
        </li>
        {% endfor %}
    </ul>
    """;

const string json = """
    {
      "products": [
        {
          "name": "Apple",
          "price": 329,
          "description": "flat-out fun"
        },
        {
          "name": "Orange",
          "price": 25,
          "description": "colorful"
        },
        {
          "name": "Banana",
          "price": 99,
          "description": "peel it"
        }
      ]
    }
    """;
// for (var i = 0; i < 10000; i++) {
// var result = Templater.Templater.CreateHtml(template, json);
// _ = result;
// }
var result = Templater.Templater.CreateHtml(template, json);
Console.WriteLine(result);

// BenchmarkRunner.Run<Bench>();

[MemoryDiagnoser]
public class Bench {
    const string Template = """
        <ul id="products">
            {% for product in products %}
            <li>
                <h2>{{product.name}}</h2>
                <h2>{{product.name}}</h2>
                <h2>{{product.name}}</h2>
                <h2>{{product.name}}</h2>
                <h2>{{product.name}}</h2>
                <h2>{{product.name}}</h2>
                <h2>{{product.name}}</h2>
                <h2>{{product.name}}</h2>
                <h2>{{product.name}}</h2>
                Only {{product.price | price }}
                Only {{product.price | price }}
                Only {{product.price | price }}
                Only {{product.price | price }}
                Only {{product.price | price }}
                Only {{product.price | price }}
                Only {{product.price | price }}
                Only {{product.price | price }}
                Only {{product.price | price }}
                Only {{product.price | price }}
                {{product.description | paragraph }}
                {{product.description | paragraph }}
                {{product.description | paragraph }}
                {{product.description | paragraph }}
                {{product.description | paragraph }}
                {{product.description | paragraph }}
                {{product.description | paragraph }}
                {{product.description | paragraph }}
                {{product.description | paragraph }}
                {{product.description | paragraph }}
                {{product.description | paragraph }}
            </li>
            {% endfor %}
        </ul>
        """;

    const string Json = """
        {
          "products": [
            {
              "name": "Apple",
              "price": 329,
              "description": "flat-out fun"
            },
            {
              "name": "Orange",
              "price": 25,
              "description": "colorful"
            },
            {
              "name": "Banana",
              "price": 99,
              "description": "peel it"
            }
          ]
        }
        """;

    [Benchmark]
    public string Test() => Templater.Templater.CreateHtml(Template, Json);
}
