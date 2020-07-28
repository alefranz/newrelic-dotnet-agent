namespace NewRelic.Agent.Core.NewRelic.Agent.Core.Database
{
    public static class SqlParsingCrossAgentTestJson
    {

        // Doesn't parse these cases with no whitespace after the "from":
        //   {""input"":""SELECT * FROM(foobar)"",                                  ""operation"":""select"", ""table"":""foobar""}
        //   {""input"":""SELECT * FROM(SELECT * FROM foobar) WHERE x > y"",        ""operation"":""select"", ""table"":""(subquery)""}


        public const string TestCases =
@"[
  {""input"":""SELECT * FROM foobar"",                                   ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT F FROM foobar"",                                   ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT Ff FROM foobar"",                                  ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT I FROM foobar"",                                   ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT FROMM FROM foobar"",                               ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT * FROM foobar WHERE x > y"",                       ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT * FROM `foobar`"",                                 ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT * FROM `foobar` WHERE x > y"",                     ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT * FROM database.foobar"",                          ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT * FROM database.foobar WHERE x > y"",              ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT * FROM `database`.foobar"",                        ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT * FROM `database`.foobar WHERE x > y"",            ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT * FROM database.`foobar`"",                        ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT * FROM database.`foobar` WHERE x > y"",            ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT * FROM (foobar)"",                                 ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT * FROM (foobar) WHERE x > y"",                     ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT * FROM (`foobar`)"",                               ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT * FROM (`foobar`) WHERE x > y"",                   ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT * FROM (SELECT * FROM foobar)"",                   ""operation"":""select"", ""table"":""(subquery)""},
  {""input"":""SELECT * FROM (SELECT * FROM foobar) WHERE x > y"",       ""operation"":""select"", ""table"":""(subquery)""},
  {""input"":""SELECT xy,zz,y FROM foobar"",                             ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT xy,zz,y FROM foobar ORDER BY zy"",                 ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT xy,zz,y FROM `foobar`"",                           ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT xy,zz,y FROM `foobar` ORDER BY zy"",               ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT `xy`,`zz`,y FROM foobar"",                         ""operation"":""select"", ""table"":""foobar""},
  {""input"":""SELECT Name FROM `world`.`City` WHERE Population > ?"",   ""operation"":""select"", ""table"":""City""},
  {""input"":""SELECT frok FROM `world`.`City` WHERE Population > ?"",   ""operation"":""select"", ""table"":""City""},
  {""input"":""SELECT irom FROM `world`.`City` WHERE Population > ?"",   ""operation"":""select"", ""table"":""City""},
  {""input"":""SELECT fromm FROM `world`.`City` WHERE Population > ?"",  ""operation"":""select"", ""table"":""City""},
  {""input"":""SELECT * FROM foo,bar"",                                  ""operation"":""select"", ""table"":""foo""},
  {""input"":""  \tSELECT * from \""foo\"" WHERE a = b"",                  ""operation"":""select"", ""table"":""foo""},
  {""input"":""  \tSELECT     *     \t from \""bar\"" WHERE a = b"",       ""operation"":""select"", ""table"":""bar""},
  {""input"":""SELECT FROM_UNIXTIME() from \""bar\"""",                    ""operation"":""select"", ""table"":""bar""},
  {""input"":""SELECT ffrom from \""frome\"""",                            ""operation"":""select"", ""table"":""frome""},
  {""input"":""SELECT ffrom from (\""frome\"")"",                          ""operation"":""select"", ""table"":""frome""},

  {""input"":""UPDATE abc SET x=1, y=2"",                                ""operation"":""update"", ""table"":""abc""},
  {""input"":""    \tUPDATE abc SET ffrom='iinto'"",                     ""operation"":""update"", ""table"":""abc""},
  {""input"":""    \tUPDATE 'abc' SET ffrom='iinto'"",                   ""operation"":""update"", ""table"":""abc""},
  {""input"":""    \tUPDATE `abc` SET ffrom='iinto'"",                   ""operation"":""update"", ""table"":""abc""},
  {""input"":""    \tUPDATE \""abc\"" SET ffrom='iinto'"",                 ""operation"":""update"", ""table"":""abc""},
  {""input"":""    \tUPDATE\r\tabc SET ffrom='iinto'"",                  ""operation"":""update"", ""table"":""abc""},

  {""input"":""INSERT INTO foobar (x,y) VALUES (1,2)"",                  ""operation"":""insert"", ""table"":""foobar""},
  {""input"":""INSERT INTO foobar(x,y) VALUES (1,2)"",                   ""operation"":""insert"", ""table"":""foobar""},

  {""input"":"" /* a */ SELECT * FROM alpha"",                           ""operation"":""select"", ""table"":""alpha""},
  {""input"":""SELECT /* a */ * FROM alpha"",                            ""operation"":""select"", ""table"":""alpha""},
  {""input"":""SELECT * /* a */ FROM alpha"",                            ""operation"":""select"", ""table"":""alpha""},
  {""input"":""SELECT * FROM /* a */ alpha"",                            ""operation"":""select"", ""table"":""alpha""},
  {""input"":""/* X */ SELECT /* Y */ foo/**/ FROM /**/alpha/**/"",      ""operation"":""select"", ""table"":""alpha""},

  {""input"":""mystoredprocedure'123'"",      ""operation"":""other"", ""table"":null},
  {""input"":""mystoredprocedure\t'123'"",      ""operation"":""other"", ""table"":null},
  {""input"":""mystoredprocedure\r'123'"",      ""operation"":""other"", ""table"":null},
  {""input"":""[mystoredprocedure]123"",      ""operation"":""other"", ""table"":null},
  {""input"":""\""mystoredprocedure\""abc"",      ""operation"":""other"", ""table"":null},
  {""input"":""mystoredprocedure"",      ""operation"":""other"", ""table"":null}
]";
    }
}
