//
// Program.cs
//
// Author:
//       Matt Ward <ward.matt@gmail.com>
//
// Copyright (c) 2016 Matthew Ward
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;

namespace LanguageServices
{
	public class Program
	{
		static void Main (string[] args)
		{
			try {
				var program = new Program ();
				program.Run ();
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}

		LanguageServiceClient server;

		void Run ()
		{
			server = new LanguageServiceClient ();
			server.Start ();

			UpdateJsonSchemaAssociations ();

			// Request handlers defined by json language service
			// 1 - initialize,
			// 2 - shutdown,
			// 3 - textDocument/completion,
			// 4 - completionItem/resolve,
			// 5 - textDocument/hover,
			// 6 - textDocument/documentSymbol,
			// 7 - textDocument/formatting,
			// 8 - textDocument/rangeFormatting
			var initParams = new InitializeParams {
				processId = System.Diagnostics.Process.GetCurrentProcess ().Id,
				rootPath = Path.GetDirectoryName (GetType ().Assembly.Location),
				initializationOptions = new InitializationOptions {
					languageIds = "json"
				}
			};

			var init = new RequestMessage {
				method = "initialize",
				@params = initParams
			};

			server.SendMessage (init);

			string fileName = Path.Combine (initParams.rootPath, "package.json");

			var document = new TextDocumentItem {
				languageid = "json",
				uri = fileName,
				version = 1,
				text = File.ReadAllText (fileName)
			};

			// Not supported by json language service.
			var didOpenTextDocumentParams = new DidOpenTextDocumentParams {
				uri = fileName,
				textDocument = document
			};

			var openDocument = new NotificationMessage {
				method = "textDocument/didOpen",
				@params = didOpenTextDocumentParams
			};

			server.SendMessage (openDocument);
			/*
			var didChangeTextDocumentParams = new DidChangeTextDocumentParams {
				uri = fileName,
				contentChanges = new TextDocumentContentChangeEvent[] {
					new TextDocumentContentChangeEvent {
						text = document.text
					}
				}
			};

			var openDocument = new RequestMessage {
				method = "textDocument/didChange",
				@params = didChangeTextDocumentParams
			};

			server.SendMessage (openDocument);

			var watchFilesChangedParams = new DidChangeWatchedFilesParams {
				changes = new FileEvent[] {
					new FileEvent {
						uri = fileName,
						type = FileChangeType.Created
					}
				}
			};

			var watchFilesChanged = new RequestMessage {
				method = "workspace/didChangeWatchedFiles",
				@params = watchFilesChangedParams
			};

			server.SendMessage (watchFilesChanged);
*/
			var positionParams = new TextDocumentPositionParams {
				textDocument = new TextDocumentIdentifier {
					uri = fileName
				},
				position = new Position {
					line = 1,
					character = 1
				}
			};

			var completion = new RequestMessage {
				id = 2,
				method = "textDocument/completion",
				@params = positionParams
			};

			server.SendMessage (completion);
		
			//while (true) {

			//	string line = Console.ReadLine ();
			//	var request = new RequestMessage {
			//		id = 1,
			//		method = line
			//	};
			//	server.SendMessage (request);
			//}

			System.Threading.Thread.Sleep (1000);
			Console.WriteLine ("Press a key to quit.");
			Console.ReadKey ();

			var shutdownMessage = new RequestMessage {
				id = 3,
				method = "shutdown"
			};
			server.SendMessage (shutdownMessage);

			server.Stop ();
		}

		void UpdateJsonSchemaAssociations ()
		{
			string directory = Path.GetDirectoryName (GetType ().Assembly.Location);
			string schema = Path.Combine (directory, "Schemas", "package.json.schema");

			var associations = new Dictionary<string, string[]> ();

			associations["package.json"] = new string[] { new Uri (schema).ToString () };

			var schemaNotification = new NotificationMessage {
				method = "json/schemaAssociations",
				@params = associations
			};

			server.SendMessage (schemaNotification);
		}
	}
}

