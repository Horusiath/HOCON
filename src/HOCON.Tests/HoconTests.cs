﻿//-----------------------------------------------------------------------
// <copyright file="HoconTests.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Hocon.Tests
{
    public class HoconTests
    {
        [Fact]
        public void CanUnwrapSubConfig() //undefined behavior in spec, this does not behave the same as JVM hocon.
        {
            var hocon = @"
a {
   b {
     c = 1
     d = true
   }
}";
            var config = ConfigurationFactory.ParseString(hocon).Root.GetObject().Unwrapped;
            var a = config["a"] as IDictionary<string, object>;
            var b = a["b"] as IDictionary<string, object>;
            (b["c"] as HoconValue).GetInt().Should().Be(1);
            (b["d"] as HoconValue).GetBoolean().Should().Be(true);
        }

        [Fact]
        public void ThrowsParserExceptionOnUnterminatedObject() //undefined behavior in spec
        {
            var hocon = " root { string : \"hello\" ";
            Assert.Throws<HoconParserException>(() => 
                ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsParserExceptionOnUnterminatedNestedObject() //undefined behavior in spec
        {
            var hocon = " root { bar { string : \"hello\" } ";
            Assert.Throws<HoconParserException>(() =>
                ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsParserExceptionOnUnterminatedString() //undefined behavior in spec
        {
            var hocon = " string : \"hello";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsParserExceptionOnUnterminatedStringInObject() //undefined behavior in spec
        {
            var hocon = " root { string : \"hello }";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsParserExceptionOnUnterminatedArray() //undefined behavior in spec
        {
            var hocon = " array : [1,2,3";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsParserExceptionOnUnterminatedArrayInObject() //undefined behavior in spec
        {
            var hocon = " root { array : [1,2,3 }";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void GettingStringFromArrayReturnsNull() //undefined behavior in spec
        {
            var hocon = " array : [1,2,3]";
            ConfigurationFactory.ParseString(hocon).GetString("array").Should().Be(null);
        }

        //TODO: not sure if this is the expected behavior but it is what we have established in Akka.NET
        [Fact]
        public void GettingArrayFromLiteralsReturnsNull() //undefined behavior in spec
        {
            var hocon = " literal : a b c";
            var res = ConfigurationFactory.ParseString(hocon).GetStringList("literal");

            res.Should().BeEmpty();
        }

        //Added tests to conform to the HOCON spec https://github.com/typesafehub/config/blob/master/HOCON.md
        [Fact]
        public void CanUsePathsAsKeys_3_14()
        {
            var hocon1 = @"3.14 : 42";
            var hocon2 = @"3 { 14 : 42}";
            Assert.Equal(ConfigurationFactory.ParseString(hocon1).GetString("3.14"),
                ConfigurationFactory.ParseString(hocon2).GetString("3.14"));
        }

        [Fact]
        public void CanUsePathsAsKeys_3()
        {
            var hocon1 = @"3 : 42";
            var hocon2 = @"""3"" : 42";
            Assert.Equal(ConfigurationFactory.ParseString(hocon1).GetString("3"),
                ConfigurationFactory.ParseString(hocon2).GetString("3"));
        }

        [Fact]
        public void CanUsePathsAsKeys_true()
        {
            var hocon1 = @"true : 42";
            var hocon2 = @"""true"" : 42";
            Assert.Equal(ConfigurationFactory.ParseString(hocon1).GetString("true"),
                ConfigurationFactory.ParseString(hocon2).GetString("true"));
        }

        [Fact]
        public void CanUsePathsAsKeys_FooBar()
        {
            var hocon1 = @"foo.bar : 42";
            var hocon2 = @"foo { bar : 42 }";
            Assert.Equal(ConfigurationFactory.ParseString(hocon1).GetString("foo.bar"),
                ConfigurationFactory.ParseString(hocon2).GetString("foo.bar"));
        }

        [Fact]
        public void CanUsePathsAsKeys_FooBarBaz()
        {
            var hocon1 = @"foo.bar.baz : 42";
            var hocon2 = @"foo { bar { baz : 42 } }";
            Assert.Equal(ConfigurationFactory.ParseString(hocon1).GetString("foo.bar.baz"),
                ConfigurationFactory.ParseString(hocon2).GetString("foo.bar.baz"));
        }

        [Fact]
        public void CanUsePathsAsKeys_AX_AY()
        {
            var hocon1 = @"a.x : 42, a.y : 43";
            var hocon2 = @"a { x : 42, y : 43 }";
            Assert.Equal(ConfigurationFactory.ParseString(hocon1).GetString("a.x"),
                ConfigurationFactory.ParseString(hocon2).GetString("a.x"));
            Assert.Equal(ConfigurationFactory.ParseString(hocon1).GetString("a.y"),
                ConfigurationFactory.ParseString(hocon2).GetString("a.y"));
        }

        [Fact]
        public void CanUsePathsAsKeys_A_B_C()
        {
            var hocon1 = @"a b c : 42";
            var hocon2 = @"""a b c"" : 42";
            Assert.Equal(ConfigurationFactory.ParseString(hocon1).GetString("a b c"),
                ConfigurationFactory.ParseString(hocon2).GetString("a b c"));
        }


        [Fact]
        public void CanConcatenateSubstitutedUnquotedString()
        {
            var hocon = @"a {
  name = Roger
  c = Hello my name is ${a.name}
}";
            Assert.Equal("Hello my name is Roger", ConfigurationFactory.ParseString(hocon).GetString("a.c"));
        }

        [Fact]
        public void CanConcatenateSubstitutedArray()
        {
            var hocon = @"a {
  b = [1,2,3]
  c = ${a.b} [4,5,6]
}";
            Assert.True(new[] {1, 2, 3, 4, 5, 6}.SequenceEqual(ConfigurationFactory.ParseString(hocon).GetIntList("a.c")));
        }

        [Fact]
        public void SubtitutedArrayWithQuestionMarkShouldFailSilently()
        {
            var hocon = @"a {
  c = ${?a.b} [4,5,6]
}";
            Assert.True(new[] { 4, 5, 6 }.SequenceEqual(ConfigurationFactory.ParseString(hocon).GetIntList("a.c")));
        }

        [Fact]
        public void SubstitutionWithQuestionMarkShouldFailSilently()
        {
            var hocon = @"a {
  b = ${?a.c}
}";
            ConfigurationFactory.ParseString(hocon);
        }

        [Fact]
        public void SubstitutionWithQuestionMarkShouldFallbackToEnvironmentVariables()
        {
            var hocon = @"a {
  b = ${?MY_ENV_VAR}
}";
            var value = "Environment_Var";
            Environment.SetEnvironmentVariable("MY_ENV_VAR", value);
            try
            {
                Assert.Equal(value, ConfigurationFactory.ParseString(hocon).GetString("a.b"));
            }
            finally
            {
                Environment.SetEnvironmentVariable("MY_ENV_VAR", null);
            }
        }

        [Fact]
        public void CanParseSubConfig()
        {
            var hocon = @"
a {
   b {
     c = 1
     d = true
   }
}";
            var config = ConfigurationFactory.ParseString(hocon);
            var subConfig = config.GetConfig("a");
            Assert.Equal(1, subConfig.GetInt("b.c"));
            Assert.True(subConfig.GetBoolean("b.d"));
        }

        [Fact]
        public void CanParseHocon()
        {
            var hocon = @"
root {
  int = 1
  quoted-string = ""foo""
  unquoted-string = bar
  concat-string = foo bar
  object {
    hasContent = true
  }
  array = [1,2,3,4]
  array-concat = [[1,2] [3,4]]
  array-single-element = [1 2 3 4]
  array-newline-element = [
    1
    2
    3
    4
  ]
  null = null
  double = 1.23
  bool = true
}
";
            var config = ConfigurationFactory.ParseString(hocon);
            Assert.Equal("1", config.GetString("root.int"));
            Assert.Equal("1.23", config.GetString("root.double"));
            Assert.Equal(true, config.GetBoolean("root.bool"));
            Assert.Equal(true, config.GetBoolean("root.object.hasContent"));
            Assert.Equal(null, config.GetString("root.null"));
            Assert.Equal("foo", config.GetString("root.quoted-string"));
            Assert.Equal("bar", config.GetString("root.unquoted-string"));
            Assert.Equal("foo bar", config.GetString("root.concat-string"));
            Assert.True(
                new[] {1, 2, 3, 4}.SequenceEqual(ConfigurationFactory.ParseString(hocon).GetIntList("root.array")));
            Assert.True(
                new[] {1, 2, 3, 4}.SequenceEqual(
                    ConfigurationFactory.ParseString(hocon).GetIntList("root.array-newline-element")));
            Assert.True(
                new[] {"1 2 3 4"}.SequenceEqual(
                    ConfigurationFactory.ParseString(hocon).GetStringList("root.array-single-element")));
        }

        [Fact]
        public void CanParseJson()
        {
            var hocon = @"
""root"" : {
  ""int"" : 1,
  ""string"" : ""foo"",
  ""object"" : {
        ""hasContent"" : true
    },
  ""array"" : [1,2,3],
  ""null"" : null,
  ""double"" : 1.23,
  ""bool"" : true
}
";
            var config = ConfigurationFactory.ParseString(hocon);
            Assert.Equal("1", config.GetString("root.int"));
            Assert.Equal("1.23", config.GetString("root.double"));
            Assert.Equal(true, config.GetBoolean("root.bool"));
            Assert.Equal(true, config.GetBoolean("root.object.hasContent"));
            Assert.Equal(null, config.GetString("root.null"));
            Assert.Equal("foo", config.GetString("root.string"));
            Assert.True(new[] {1, 2, 3}.SequenceEqual(ConfigurationFactory.ParseString(hocon).GetIntList("root.array")));
        }

        [Fact]
        public void CanMergeObject()
        {
            var hocon = @"
a.b.c = {
        x = 1
        y = 2
    }
a.b.c = {
        z = 3
    }
";
            var config = ConfigurationFactory.ParseString(hocon);
            Assert.Equal("1", config.GetString("a.b.c.x"));
            Assert.Equal("2", config.GetString("a.b.c.y"));
            Assert.Equal("3", config.GetString("a.b.c.z"));
        }

        [Fact]
        public void CanOverrideObject()
        {
            var hocon = @"
a.b = 1
a = null
a.c = 3
";
            var config = ConfigurationFactory.ParseString(hocon);
            Assert.Null(config.GetString("a.b"));
            Assert.Equal("3", config.GetString("a.c"));
        }

        [Fact]
        public void CanParseObject()
        {
            var hocon = @"
a {
  b = 1
}
";
            Assert.Equal("1", ConfigurationFactory.ParseString(hocon).GetString("a.b"));
        }

        [Fact]
        public void CanTrimValue()
        {
            var hocon = "a= \t \t 1 \t \t,";
            Assert.Equal("1", ConfigurationFactory.ParseString(hocon).GetString("a"));
        }

        [Fact]
        public void CanTrimConcatenatedValue()
        {
            var hocon = "a= \t \t 1 2 3 \t \t,";
            Assert.Equal("1 2 3", ConfigurationFactory.ParseString(hocon).GetString("a"));
        }

        [Fact]
        public void CanConsumeCommaAfterValue()
        {
            var hocon = "a=1,";
            Assert.Equal("1", ConfigurationFactory.ParseString(hocon).GetString("a"));
        }

        [Fact]
        public void CanAssignIpAddressToField()
        {
            var hocon = @"a=127.0.0.1";
            Assert.Equal("127.0.0.1", ConfigurationFactory.ParseString(hocon).GetString("a"));
        }

        [Fact]
        public void CanAssignConcatenatedValueToField()
        {
            var hocon = @"a=1 2 3";
            Assert.Equal("1 2 3", ConfigurationFactory.ParseString(hocon).GetString("a"));
        }

        [Fact]
        public void CanAssignValueToQuotedField()
        {
            var hocon = @"""a""=1";
            Assert.Equal(1L, ConfigurationFactory.ParseString(hocon).GetLong("a"));
        }

        [Fact]
        public void CanAssignValueToPathExpression()
        {
            var hocon = @"a.b.c=1";
            Assert.Equal(1L, ConfigurationFactory.ParseString(hocon).GetLong("a.b.c"));
        }

        [Fact]
        public void CanAssignValuesToPathExpressions()
        {
            var hocon = @"
a.b.c=1
a.b.d=2
a.b.e.f=3
";
            var config = ConfigurationFactory.ParseString(hocon);
            Assert.Equal(1L, config.GetLong("a.b.c"));
            Assert.Equal(2L, config.GetLong("a.b.d"));
            Assert.Equal(3L, config.GetLong("a.b.e.f"));
        }

        [Fact]
        public void CanAssignLongToField()
        {
            var hocon = @"a=1";
            Assert.Equal(1L, ConfigurationFactory.ParseString(hocon).GetLong("a"));
        }

        [Fact]
        public void CanAssignArrayToField()
        {
            var hocon = @"a=
[
    1
    2
    3
]";
            Assert.True(new[] {1, 2, 3}.SequenceEqual(ConfigurationFactory.ParseString(hocon).GetIntList("a")));

            //hocon = @"a= [ 1, 2, 3 ]";
            //Assert.True(new[] { 1, 2, 3 }.SequenceEqual(ConfigurationFactory.ParseString(hocon).GetIntList("a")));
        }

        [Fact]
        public void CanConcatenateArray()
        {
            var hocon = @"a=[1,2] [3,4]";
            Assert.True(new[] {1, 2, 3, 4}.SequenceEqual(ConfigurationFactory.ParseString(hocon).GetIntList("a")));
        }

        [Fact]
        public void CanAssignSubstitutionToField()
        {
            var hocon = @"a{
    b = 1
    c = ${a.b}
    d = ${a.c}23
}";
            Assert.Equal(1, ConfigurationFactory.ParseString(hocon).GetInt("a.c"));
            Assert.Equal(123, ConfigurationFactory.ParseString(hocon).GetInt("a.d"));
        }

        [Fact]
        public void CanAssignDoubleToField()
        {
            var hocon = @"a=1.1";
            Assert.Equal(1.1, ConfigurationFactory.ParseString(hocon).GetDouble("a"));
        }

        [Fact]
        public void CanAssignNullToField()
        {
            var hocon = @"a=null";
            Assert.Null(ConfigurationFactory.ParseString(hocon).GetString("a"));
        }

        [Fact]
        public void CanAssignBooleanToField()
        {
            var hocon = @"a=true";
            Assert.True(ConfigurationFactory.ParseString(hocon).GetBoolean("a"));
            hocon = @"a=false";
            Assert.False(ConfigurationFactory.ParseString(hocon).GetBoolean("a"));

            hocon = @"a=on";
            Assert.True(ConfigurationFactory.ParseString(hocon).GetBoolean("a"));
            hocon = @"a=off";
            Assert.False(ConfigurationFactory.ParseString(hocon).GetBoolean("a"));
        }

        [Fact]
        public void CanAssignQuotedStringToField()
        {
            var hocon = @"a=""hello""";
            Assert.Equal("hello", ConfigurationFactory.ParseString(hocon).GetString("a"));
        }

        [Fact]
        public void CanAssignTrippleQuotedStringToField()
        {
            var hocon = @"a=""""""hello""""""";
            Assert.Equal("hello", ConfigurationFactory.ParseString(hocon).GetString("a"));
        }

        [Fact]
        public void CanAssignUnQuotedStringToField()
        {
            var hocon = @"a=hello";
            Assert.Equal("hello", ConfigurationFactory.ParseString(hocon).GetString("a"));
        }

        [Fact]
        public void CanAssignTripleQuotedStringToField()
        {
            var hocon = @"a=""""""hello""""""";
            Assert.Equal("hello", ConfigurationFactory.ParseString(hocon).GetString("a"));
        }

        [Fact]
        public void CanUseFallback()
        {
            var hocon1 = @"
foo {
   bar {
      a=123
   }
}";
            var hocon2 = @"
foo {
   bar {
      a=1
      b=2
      c=3
   }
}";

            var config1 = ConfigurationFactory.ParseString(hocon1);
            var config2 = ConfigurationFactory.ParseString(hocon2);

            var config = config1.WithFallback(config2);

            Assert.Equal(123, config.GetInt("foo.bar.a"));
            Assert.Equal(2, config.GetInt("foo.bar.b"));
            Assert.Equal(3, config.GetInt("foo.bar.c"));
        }

        [Fact]
        public void CanUseFallbackInSubConfig()
        {
            var hocon1 = @"
foo {
   bar {
      a=123
   }
}";
            var hocon2 = @"
foo {
   bar {
      a=1
      b=2
      c=3
   }
}";

            var config1 = ConfigurationFactory.ParseString(hocon1);
            var config2 = ConfigurationFactory.ParseString(hocon2);

            var config = config1.WithFallback(config2).GetConfig("foo.bar");

            Assert.Equal(123, config.GetInt("a"));
            Assert.Equal(2, config.GetInt("b"));
            Assert.Equal(3, config.GetInt("c"));
        }

        [Fact]
        public void CanUseMultiLevelFallback()
        {
            var hocon1 = @"
foo {
   bar {
      a=123
   }
}";
            var hocon2 = @"
foo {
   bar {
      a=1
      b=2
      c=3
   }
}";
            var hocon3 = @"
foo {
   bar {
      a=99
      zork=555
   }
}";
            var hocon4 = @"
foo {
   bar {
      borkbork=-1
   }
}";

            var config1 = ConfigurationFactory.ParseString(hocon1);
            var config2 = ConfigurationFactory.ParseString(hocon2);
            var config3 = ConfigurationFactory.ParseString(hocon3);
            var config4 = ConfigurationFactory.ParseString(hocon4);

            var config = config1.WithFallback(config2.WithFallback(config3.WithFallback(config4)));

            config.GetInt("foo.bar.a").Should().Be(123);
            config.GetInt("foo.bar.b").Should().Be(2);
            config.GetInt("foo.bar.c").Should().Be(3);
            config.GetInt("foo.bar.zork").Should().Be(555);
            config.GetInt("foo.bar.borkbork").Should().Be(-1);
        }

        [Fact]
        public void CanUseFluentMultiLevelFallback()
        {
            var hocon1 = @"
foo {
   bar {
      a=123
   }
}";
            var hocon2 = @"
foo {
   bar {
      a=1
      b=2
      c=3
   }
}";
            var hocon3 = @"
foo {
   bar {
      a=99
      zork=555
   }
}";
            var hocon4 = @"
foo {
   bar {
      borkbork=-1
   }
}";

            var config1 = ConfigurationFactory.ParseString(hocon1);
            var config2 = ConfigurationFactory.ParseString(hocon2);
            var config3 = ConfigurationFactory.ParseString(hocon3);
            var config4 = ConfigurationFactory.ParseString(hocon4);

            var config = config1.WithFallback(config2).WithFallback(config3).WithFallback(config4);

            config.GetInt("foo.bar.a").Should().Be(123);
            config.GetInt("foo.bar.b").Should().Be(2);
            config.GetInt("foo.bar.c").Should().Be(3);
            config.GetInt("foo.bar.zork").Should().Be(555);
            config.GetInt("foo.bar.borkbork").Should().Be(-1);
        }

        [Fact]
        public void CanParseQuotedKeys()
        {
            var hocon = @"
a {
   ""some quoted, key"": 123
}
";
            var config = ConfigurationFactory.ParseString(hocon);
            config.GetInt("a.some quoted, key").Should().Be(123);
        }

        [Fact]
        public void CanEnumerateQuotedKeys()
        {
            var hocon = @"
a {
   ""some quoted, key"": 123
}
";
            var config = ConfigurationFactory.ParseString(hocon);
            var config2 = config.GetConfig("a");
            var enumerable = config2.AsEnumerable();

            enumerable.Select(kvp => kvp.Key).First().Should().Be("some quoted, key");
        }

        [Fact]
        public void CanParseSerializersAndBindings()
        {
            var hocon = @"
akka.actor {
    serializers {
      akka-containers = ""Akka.Remote.Serialization.MessageContainerSerializer, Akka.Remote""
      proto = ""Akka.Remote.Serialization.ProtobufSerializer, Akka.Remote""
      daemon-create = ""Akka.Remote.Serialization.DaemonMsgCreateSerializer, Akka.Remote""
    }

    serialization-bindings {
      # Since com.google.protobuf.Message does not extend Serializable but
      # GeneratedMessage does, need to use the more specific one here in order
      # to avoid ambiguity
      ""Akka.Actor.ActorSelectionMessage"" = akka-containers
      ""Akka.Remote.DaemonMsgCreate, Akka.Remote"" = daemon-create
    }

}";

            var config = ConfigurationFactory.ParseString(hocon);

            var serializersConfig = config.GetConfig("akka.actor.serializers").AsEnumerable().ToList();
            var serializerBindingConfig = config.GetConfig("akka.actor.serialization-bindings").AsEnumerable().ToList();

            serializersConfig.Select(kvp => kvp.Value)
                .First()
                .GetString()
                .Should().Be("Akka.Remote.Serialization.MessageContainerSerializer, Akka.Remote");
            serializerBindingConfig.Select(kvp => kvp.Key).Last().Should().Be("Akka.Remote.DaemonMsgCreate, Akka.Remote");
        }

        [Fact]
        public void CanOverwriteValue()
        {
            var hocon = @"
test {
  value  = 123
}
test.value = 456
";
            var config = ConfigurationFactory.ParseString(hocon);
            config.GetInt("test.value").Should().Be(456);
        }

        [Fact]
        public void CanCSubstituteObject()
        {
            var hocon = @"a {
  b {
      foo = hello
      bar = 123
  }
  c {
     d = xyz
     e = ${a.b}
  }  
}";
            var ace = ConfigurationFactory.ParseString(hocon).GetConfig("a.c.e");
            Assert.Equal("hello", ace.GetString("foo"));
            Assert.Equal(123, ace.GetInt("bar"));
        }

        [Fact]
        public void CanAssignNullStringToField()
        {
            var hocon = @"a=null";
            Assert.Equal(null, ConfigurationFactory.ParseString(hocon).GetString("a"));
        }

        [Fact(Skip = "we currently do not make any destinction between quoted and unquoted strings once parsed")]
        public void CanAssignQuotedNullStringToField()
        {
            var hocon = @"a=""null""";
            Assert.Equal("null", ConfigurationFactory.ParseString(hocon).GetString("a"));
        }

        [Fact]
        public void CanParseInclude()
        {
            var hocon = @"a {
  b { 
       include ""foo""
  }";
            var includeHocon = @"
x = 123
y = hello
";
            Func<string, HoconRoot> include = s => Parser.Parse(includeHocon, null);
            var config = ConfigurationFactory.ParseString(hocon,include);

            Assert.Equal(123,config.GetInt("a.b.x"));
            Assert.Equal("hello", config.GetString("a.b.y"));
        }

        [Fact]
        public void CanResolveSubstitutesInInclude()
        {
            var hocon = @"a {
  b { 
       include ""foo""
  }";
            var includeHocon = @"
x = 123
y = ${x}
";
            Func<string, HoconRoot> include = s => Parser.Parse(includeHocon, null);
            var config = ConfigurationFactory.ParseString(hocon, include);

            Assert.Equal(123, config.GetInt("a.b.x"));
            Assert.Equal(123, config.GetInt("a.b.y"));
        }

        [Fact]
        public void CanResolveSubstitutesInNestedIncludes()
        {
            var hocon = @"a.b.c {
  d { 
       include ""foo""
  }";
            var includeHocon = @"
f = 123
e {
      include ""foo""
}
";

            var includeHocon2 = @"
x = 123
y = ${x}
";

            Func<string, HoconRoot> include2 = s => Parser.Parse(includeHocon2, null);
            Func<string, HoconRoot> include = s => Parser.Parse(includeHocon, include2);
            var config = ConfigurationFactory.ParseString(hocon, include);

            Assert.Equal(123, config.GetInt("a.b.c.d.e.x"));
            Assert.Equal(123, config.GetInt("a.b.c.d.e.y"));
        }
    }
}

