using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetDoc
{
    public class DocumentationVisitor : SymbolVisitor
    {
        public override void VisitAssembly(IAssemblySymbol symbol)
        {
            symbol.GlobalNamespace.Accept(this);
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            foreach (var child in symbol.GetMembers())
            {
                child.Accept(this);
            }
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            foreach (var member in symbol.GetMembers())
            {
                member.Accept(this);
            }
        }

        //- This is a test
        //- It has two lines
        public override void VisitMethod(IMethodSymbol symbol)
        {
            if (symbol.MethodKind == MethodKind.Ordinary)
            {
                string asm = symbol.ContainingAssembly.Name;
                string type = symbol.ContainingType.Name;
                string ns = symbol.ContainingNamespace.Name;

                System.Console.WriteLine(asm + ":" + ns + "." + type + "#" + symbol.Name);

                var declaration = symbol.DeclaringSyntaxReferences.FirstOrDefault();
                if (declaration != null)
                {
                    var comments = declaration
                        .GetSyntax()
                        .GetLeadingTrivia()
                        .Where(t => t.CSharpKind() == SyntaxKind.SingleLineCommentTrivia)
                        .Select(t => new { Trivia = t, Text = t.ToFullString() })
                        .Where(t => t.Text.StartsWith("//-"))
                        .Select(t => new { t.Trivia, Text = t.Text.Substring(3) });
                    foreach (var comment in comments)
                    {

                        System.Console.WriteLine(" " + comment.Text);
                    }
                }

            }
            base.VisitMethod(symbol);
        }
    }
}