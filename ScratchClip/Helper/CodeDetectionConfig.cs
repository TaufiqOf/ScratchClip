using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ScratchClip.ViewModels;

namespace ScratchClip.Helper;

public static class CodeDetectionConfig
{
    // --------------------------------------------------------
    // General detection
    // --------------------------------------------------------

    public const double MinimumConfidence = 0.65;


    // --------------------------------------------------------
    // Confidence calculation
    // --------------------------------------------------------

    public const double SyntaxWeight = 0.70;
    public const double FingerprintWeight = 0.30;


    // --------------------------------------------------------
    // Syntax score penalties
    // --------------------------------------------------------

    public const double ErrorNodePenalty = 1.5;
    public const double MissingNodePenalty = 0.75;


    // --------------------------------------------------------
    // General code heuristics
    // --------------------------------------------------------

    public const double MinimumKeywordDensity = 0.08;
    public const int MinimumKeywordMatches = 2;
    public const int MinimumMultilineSymbols = 2;
    public const int MinimumMultilineKeywords = 1;
    public const int MinimumSymbolMatches = 1;
    public const int MinimumOperatorMatches = 1;


    // --------------------------------------------------------
    // Natural prose detection
    // --------------------------------------------------------

    public const int ProseSentenceCount = 2;
    public const double ProseLetterSpaceRatio = 0.90;
    public const int ProseMaxCodePunctuation = 2;
    public const int LongProseWordCount = 15;
    public const int MinimumProseSentences = 1;
    public const int LongProseMaxSymbols = 5;


    // --------------------------------------------------------
    // Regular expressions
    // --------------------------------------------------------

    public const string ScalarRegex =
        @"^(?:[+-]?\d+(?:\.\d+)?|true|false|null)$";

    public const string SentenceRegex =
        @"\b[A-Z][^.!?]*[.!?]";

    public const string PlainIdentifierRegex =
        @"^\w+$";

    public const string KeywordRegex =
        @"\b(class|interface|struct|enum|namespace|public|private|protected|internal|using|return|function|def|let|const|var|import|export|async|await|new|void|static|fn|func|package|impl|trait)\b";

    public const string StrongSymbolRegex =
        @"(=>|==|!=|<=|>=|\{|\}|\[|\]|;)";

    public const string OperatorRegex =
        @"(=>|==|!=|<=|>=|&&|\|\||\+\+|--|\+=|-=|\*=|/=)";

    public const string StrongCodePatternRegex =
        """
        ^\s*
        (
            (public|private|protected|internal)\s+
                (class|interface|struct|enum|record)\b

            |

            (if|for|foreach|while|switch|try|catch)\s*\(

            |

            (using|namespace)\s+[\w.]+

            |

            (def|func|function|fn)\s+\w+

            |

            (package|import)\s+[\w."]

            |

            #\s*(include|define)\b
        )
        """;


    // ========================================================
    // DEFAULT LANGUAGE DEFINITIONS
    // ========================================================

    public static readonly IReadOnlyList<LanguageDefinition> DefaultLanguages =
    [
        // ----------------------------------------------------
        // C#
        // ----------------------------------------------------

        new()
        {
            Id = "csharp",
            Name = "C#",
            Extension = ".cs",
            NativeLibrary = "libtree-sitter-c-sharp.so",
            FunctionName = "tree_sitter_c_sharp",
            Keywords =
            [
                "using ",
                "namespace ",
                "public ",
                "private ",
                "protected ",
                "internal ",
                "class ",
                "interface ",
                "record ",
                "struct ",
                "enum ",
                "Console.",
                "async ",
                "await ",
                "var ",
                "string ",
                "int ",
                "bool ",
                "Task<",
                "=>"
            ]
        },


        // ----------------------------------------------------
        // Python
        // ----------------------------------------------------

        new()
        {
            Id = "python",
            Name = "PYTHON",
            Extension = ".py",
            NativeLibrary = "libtree-sitter-python.so",
            FunctionName = "tree_sitter_python",
            Keywords =
            [
                "def ",
                "async def ",
                "import ",
                "from ",
                "elif ",
                "None",
                "True",
                "False",
                "self",
                "print(",
                "__init__",
                "__name__",
                "lambda ",
                "yield ",
                "with ",
                "except "
            ]
        },


        // ----------------------------------------------------
        // JavaScript
        // ----------------------------------------------------

        new()
        {
            Id = "javascript",
            Name = "JAVASCRIPT",
            Extension = ".js",
            NativeLibrary = "libtree-sitter-javascript.so",
            FunctionName = "tree_sitter_javascript",
            Keywords =
            [
                "const ",
                "let ",
                "function ",
                "console.",
                "require(",
                "module.exports",
                "export ",
                "import ",
                "=>",
                "undefined",
                "Promise",
                "async ",
                "await ",
                "document.",
                "window."
            ]
        },


        // ----------------------------------------------------
        // Java
        // ----------------------------------------------------

        new()
        {
            Id = "java",
            Name = "JAVA",
            Extension = ".java",
            NativeLibrary = "libtree-sitter-java.so",
            FunctionName = "tree_sitter_java",
            Keywords =
            [
                "public class ",
                "private class ",
                "protected class ",
                "System.out.",
                "package ",
                "import java.",
                "implements ",
                "extends ",
                "public static void ",
                "private static ",
                "interface ",
                "new ",
                "throws ",
                "boolean ",
                "String "
            ]
        },


        // ----------------------------------------------------
        // C
        // ----------------------------------------------------

        new()
        {
            Id = "c",
            Name = "C",
            Extension = ".c",
            NativeLibrary = "libtree-sitter-c.so",
            FunctionName = "tree_sitter_c",
            Keywords =
            [
                "#include ",
                "#define ",
                "int main(",
                "printf(",
                "scanf(",
                "malloc(",
                "free(",
                "NULL",
                "typedef ",
                "struct ",
                "sizeof("
            ]
        },


        // ----------------------------------------------------
        // C++
        // ----------------------------------------------------

        new()
        {
            Id = "cpp",
            Name = "C++",
            Extension = ".cpp",
            NativeLibrary = "libtree-sitter-cpp.so",
            FunctionName = "tree_sitter_cpp",
            Keywords =
            [
                "#include ",
                "#define ",
                "std::",
                "cout",
                "cin",
                "nullptr",
                "public:",
                "private:",
                "protected:",
                "template<",
                "class ",
                "namespace ",
                "vector<",
                "string ",
                "using namespace "
            ]
        },


        // ----------------------------------------------------
        // Go
        // ----------------------------------------------------

        new()
        {
            Id = "go",
            Name = "GO",
            Extension = ".go",
            NativeLibrary = "libtree-sitter-go.so",
            FunctionName = "tree_sitter_go",
            Keywords =
            [
                "package main",
                "package ",
                "import (",
                "import \"",
                "func ",
                "go ",
                "defer ",
                "chan ",
                "struct {",
                "interface {",
                "fmt.",
                ":=",
                "make(",
                "range "
            ]
        },


        // ----------------------------------------------------
        // Rust
        // ----------------------------------------------------

        new()
        {
            Id = "rust",
            Name = "RUST",
            Extension = ".rs",
            NativeLibrary = "libtree-sitter-rust.so",
            FunctionName = "tree_sitter_rust",
            Keywords =
            [
                "fn ",
                "let mut ",
                "let ",
                "pub fn ",
                "impl ",
                "trait ",
                "struct ",
                "enum ",
                "use ",
                "mod ",
                "crate::",
                "println!(",
                "vec![",
                "match ",
                "Some(",
                "None",
                "Result<",
                "Option<"
            ]
        }
    ];


    // ========================================================
    // ACTIVE / EDITABLE LANGUAGE DEFINITIONS
    // ========================================================

    public static ObservableCollection<LanguageDefinition> Languages { get; set; }
        = new ObservableCollection<LanguageDefinition>(DefaultLanguages.Select(CloneLanguage));


    // ========================================================
    // HELPERS
    // ========================================================

    private static LanguageDefinition CloneLanguage(
        LanguageDefinition source)
    {
        return new LanguageDefinition
        {
            Id = source.Id,
            Name = source.Name,
            Extension = source.Extension,
            NativeLibrary = source.NativeLibrary,
            FunctionName = source.FunctionName,
            Keywords = new ObservableCollection<string>(
                source.Keywords)
        };
    }


    // ========================================================
    // LANGUAGE DEFINITION
    // ========================================================

    public sealed class LanguageDefinition : ViewModelBase
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Extension { get; set; } = string.Empty;

        public string NativeLibrary { get; set; } = string.Empty;

        public string FunctionName { get; set; } = string.Empty;

        public ObservableCollection<string> Keywords { get; set; } = [];
    }
}