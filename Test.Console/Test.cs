using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Templater;

namespace Test.Console;

public class Test {
    public static void Foo() {
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
        // var t = template.AsSpan();
        // while (t.Length > 0) {
        //     var parseResult = ParseHelper.ParseToNextInlineBlock(t, out var offset, out var length, out var type);
        //     _ = parseResult;
        //     System.Console.WriteLine($"type: {type}; '{t[offset..(offset + length)]}'");
        //     t = t[(offset + length)..];
        // }
        // BenchmarkRunner.Run<Bench>();
        if (ParseHelper.ExtractNamesFromForStart("{% for product in products %}".AsSpan().Trim(" {}%"), out var objectName, out var collectionPath)) {
            System.Console.WriteLine($"objectName: '{objectName}'; collectionPath: '{collectionPath}'");
        }
    }
}

[MemoryDiagnoser]
public class Bench {
    const string Template = """
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

    readonly char[] ar = new char[Template.Length * 100];

    public Bench() {
        for (var i = 0; i < 100; i++) {
            Template.AsSpan().CopyTo(ar.AsSpan()[(i * Template.Length)..]);
        }
    }

    [Benchmark]
    public bool Test() {
        var t = ar.AsSpan();
        while (t.Length > 0) {
            var parseResult = ParseHelper.ParseToNextInlineBlock(t, out var offset, out var length, out var type);
            _ = parseResult;
            t = t[(offset + length)..];
        }

        return true;
    }
}
