using System;
using System.Globalization;
using ParadoxDataLib.Core.Common;
using ParadoxDataLib.Core.Tokenizer;

namespace ParadoxDataLib.Core.Parsers
{
    /// <summary>
    /// Generic parser that can parse any Paradox game file format into a tree structure.
    /// This parser handles all common Paradox patterns: key-value pairs, nested objects,
    /// lists, dates, and comments.
    /// </summary>
    public class GenericParadoxParser : BaseParser<ParadoxNode>
    {
        /// <summary>
        /// Initializes a new instance of the GenericParadoxParser
        /// </summary>
        public GenericParadoxParser() : base()
        {
        }

        /// <summary>
        /// Parses the content, handling empty content appropriately
        /// </summary>
        /// <param name="content">The content to parse</param>
        /// <returns>Parsed ParadoxNode tree, never null</returns>
        public override ParadoxNode Parse(string content)
        {
            // For empty content, return an empty root node instead of null
            if (string.IsNullOrEmpty(content))
            {
                return ParadoxNode.CreateObject("root");
            }

            return base.Parse(content);
        }

        /// <summary>
        /// Parses the tokenized content into a generic ParadoxNode tree structure
        /// </summary>
        /// <returns>Root ParadoxNode containing the entire parsed file structure</returns>
        protected override ParadoxNode ParseTokens()
        {
            var root = ParadoxNode.CreateObject("root");

            while (!IsEndOfFile())
            {
                SkipComments();

                if (IsEndOfFile()) break;

                var node = ParseNode();
                if (node != null)
                {
                    root.AddChild(node);
                }
            }

            return root;
        }

        /// <summary>
        /// Parses a single node (key-value pair, object, list, or date)
        /// </summary>
        /// <returns>The parsed ParadoxNode</returns>
        private ParadoxNode ParseNode()
        {
            var token = CurrentToken();

            // Handle date entries (e.g., "1444.11.11 = { ... }")
            if (token.Type == TokenType.Date)
            {
                var dateKey = token.Value;
                var date = ParseDate(token.Value);

                ConsumeToken(); // Consume date token
                ExpectToken(TokenType.Equals); // Consume =

                var nextToken = CurrentToken();
                if (nextToken.Type == TokenType.LeftBrace)
                {
                    // Date with object block
                    var dateNode = ParadoxNode.CreateDate(dateKey, date);
                    ConsumeToken(); // Consume {

                    // Parse the contents as an object
                    var objectContent = ParseObjectContent();
                    foreach (var child in objectContent.Children)
                    {
                        dateNode.AddChild(child.Value);
                    }

                    ExpectToken(TokenType.RightBrace); // Consume }
                    return dateNode;
                }
                else
                {
                    // Date with simple value
                    var value = ParseValue();
                    return ParadoxNode.CreateScalar(dateKey, value);
                }
            }

            // Handle regular identifiers (keys)
            if (token.Type == TokenType.Identifier)
            {
                var key = token.Value;
                ConsumeToken(); // Consume key

                if (!ExpectToken(TokenType.Equals))
                {
                    SkipToNextStatement();
                    return null;
                }

                // Determine what follows the equals sign
                var valueToken = CurrentToken();

                if (valueToken.Type == TokenType.LeftBrace)
                {
                    // Object: key = { ... }
                    ConsumeToken(); // Consume {
                    var objectNode = ParadoxNode.CreateObject(key);
                    var content = ParseObjectContent();

                    foreach (var child in content.Children)
                    {
                        objectNode.AddChild(child.Value);
                    }

                    ExpectToken(TokenType.RightBrace); // Consume }
                    return objectNode;
                }
                else
                {
                    // Scalar: key = value
                    var value = ParseValue();
                    return ParadoxNode.CreateScalar(key, value);
                }
            }

            // Skip unknown tokens
            ConsumeToken();
            return null;
        }

        /// <summary>
        /// Parses the content inside an object block (between { and })
        /// </summary>
        /// <returns>ParadoxNode containing all parsed child nodes</returns>
        private ParadoxNode ParseObjectContent()
        {
            var container = ParadoxNode.CreateObject("");

            while (!IsEndOfFile() && !IsToken(TokenType.RightBrace))
            {
                SkipComments();

                if (IsToken(TokenType.RightBrace)) break;

                var node = ParseNode();
                if (node != null)
                {
                    container.AddChild(node);
                }
            }

            return container;
        }

        /// <summary>
        /// Parses a single value (string, number, boolean, etc.)
        /// </summary>
        /// <returns>The parsed value as an object</returns>
        private object ParseValue()
        {
            var token = CurrentToken();
            ConsumeToken();

            switch (token.Type)
            {
                case TokenType.String:
                    return token.Value.Trim('"');

                case TokenType.Number:
                    // Try to parse as integer first, then float
                    if (int.TryParse(token.Value, out var intValue))
                        return intValue;
                    if (float.TryParse(token.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var floatValue))
                        return floatValue;
                    return token.Value; // Return as string if parsing fails

                case TokenType.Yes:
                    return true;

                case TokenType.No:
                    return false;

                case TokenType.Date:
                    return ParseDate(token.Value);

                case TokenType.Identifier:
                    // Check if it's a boolean-like identifier
                    var lowerValue = token.Value.ToLower();
                    if (lowerValue == "yes" || lowerValue == "true")
                        return true;
                    if (lowerValue == "no" || lowerValue == "false")
                        return false;

                    // Return as string identifier
                    return token.Value;

                default:
                    return token.Value;
            }
        }

        /// <summary>
        /// Skips to the next statement by advancing until we find a key token or end of file
        /// </summary>
        private void SkipToNextStatement()
        {
            while (!IsEndOfFile())
            {
                var token = CurrentToken();

                // Stop at potential next statement starts
                if (token.Type == TokenType.Identifier || token.Type == TokenType.Date)
                {
                    break;
                }

                // Skip balanced braces
                if (token.Type == TokenType.LeftBrace)
                {
                    SkipBalancedBraces();
                }
                else
                {
                    ConsumeToken();
                }
            }
        }

        /// <summary>
        /// Skips over a balanced set of braces { ... }
        /// </summary>
        private void SkipBalancedBraces()
        {
            if (!IsToken(TokenType.LeftBrace))
                return;

            ConsumeToken(); // Consume opening brace
            int braceCount = 1;

            while (!IsEndOfFile() && braceCount > 0)
            {
                var token = CurrentToken();
                ConsumeToken();

                if (token.Type == TokenType.LeftBrace)
                    braceCount++;
                else if (token.Type == TokenType.RightBrace)
                    braceCount--;
            }
        }
    }
}