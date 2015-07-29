namespace Bermuda.ExpressionGeneration {

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class Token {
	public int kind;    // token kind
	public int pos;     // token position in the source text (starting at 0)
	public int col;     // token column (starting at 1)
	public int line;    // token line (starting at 1)
	public string val;  // token value
	public Token next;  // ML 2005-03-11 Tokens are kept in linked list
}

//-----------------------------------------------------------------------------------
// Buffer
//-----------------------------------------------------------------------------------
public class Buffer {
	// This Buffer supports the following cases:
	// 1) seekable stream (file)
	//    a) whole stream in buffer
	//    b) part of stream in buffer
	// 2) non seekable stream (network, console)

	public const int EOF = char.MaxValue + 1;
	const int MIN_BUFFER_LENGTH = 1024; // 1KB
	const int MAX_BUFFER_LENGTH = MIN_BUFFER_LENGTH * 64; // 64KB
	byte[] buf;         // input buffer
	int bufStart;       // position of first byte in buffer relative to input stream
	int bufLen;         // length of buffer
	int fileLen;        // length of input stream (may change if the stream is no file)
	int bufPos;         // current position in buffer
	Stream stream;      // input stream (seekable)
	bool isUserStream;  // was the stream opened by the user?
	
	public Buffer (Stream s, bool isUserStream) {
		stream = s; this.isUserStream = isUserStream;
		
		if (stream.CanSeek) {
			fileLen = (int) stream.Length;
			bufLen = Math.Min(fileLen, MAX_BUFFER_LENGTH);
			bufStart = Int32.MaxValue; // nothing in the buffer so far
		} else {
			fileLen = bufLen = bufStart = 0;
		}

		buf = new byte[(bufLen>0) ? bufLen : MIN_BUFFER_LENGTH];
		if (fileLen > 0) Pos = 0; // setup buffer to position 0 (start)
		else bufPos = 0; // index 0 is already after the file, thus Pos = 0 is invalid
		if (bufLen == fileLen && stream.CanSeek) Close();
	}
	
	protected Buffer(Buffer b) { // called in UTF8Buffer constructor
		buf = b.buf;
		bufStart = b.bufStart;
		bufLen = b.bufLen;
		fileLen = b.fileLen;
		bufPos = b.bufPos;
		stream = b.stream;
		// keep destructor from closing the stream
		b.stream = null;
		isUserStream = b.isUserStream;
	}

	~Buffer() { Close(); }
	
	protected void Close() {
		if (!isUserStream && stream != null) {
			stream.Close();
			stream = null;
		}
	}
	
	public virtual int Read () {
		if (bufPos < bufLen) {
			return buf[bufPos++];
		} else if (Pos < fileLen) {
			Pos = Pos; // shift buffer start to Pos
			return buf[bufPos++];
		} else if (stream != null && !stream.CanSeek && ReadNextStreamChunk() > 0) {
			return buf[bufPos++];
		} else {
			return EOF;
		}
	}

	public int Peek () {
		int curPos = Pos;
		int ch = Read();
		Pos = curPos;
		return ch;
	}
	
	public string GetString (int beg, int end) {
		int len = 0;
		char[] buf = new char[end - beg];
		int oldPos = Pos;
		Pos = beg;
		while (Pos < end) buf[len++] = (char) Read();
		Pos = oldPos;
		return new String(buf, 0, len);
	}

	public int Pos {
		get { return bufPos + bufStart; }
		set {
			if (value >= fileLen && stream != null && !stream.CanSeek) {
				// Wanted position is after buffer and the stream
				// is not seek-able e.g. network or console,
				// thus we have to read the stream manually till
				// the wanted position is in sight.
				while (value >= fileLen && ReadNextStreamChunk() > 0);
			}

			if (value < 0 || value > fileLen) {
				throw new FatalError("buffer out of bounds access, position: " + value);
			}

			if (value >= bufStart && value < bufStart + bufLen) { // already in buffer
				bufPos = value - bufStart;
			} else if (stream != null) { // must be swapped in
				stream.Seek(value, SeekOrigin.Begin);
				bufLen = stream.Read(buf, 0, buf.Length);
				bufStart = value; bufPos = 0;
			} else {
				// set the position to the end of the file, Pos will return fileLen.
				bufPos = fileLen - bufStart;
			}
		}
	}
	
	// Read the next chunk of bytes from the stream, increases the buffer
	// if needed and updates the fields fileLen and bufLen.
	// Returns the number of bytes read.
	private int ReadNextStreamChunk() {
		int free = buf.Length - bufLen;
		if (free == 0) {
			// in the case of a growing input stream
			// we can neither seek in the stream, nor can we
			// foresee the maximum length, thus we must adapt
			// the buffer size on demand.
			byte[] newBuf = new byte[bufLen * 2];
			Array.Copy(buf, newBuf, bufLen);
			buf = newBuf;
			free = bufLen;
		}
		int read = stream.Read(buf, bufLen, free);
		if (read > 0) {
			fileLen = bufLen = (bufLen + read);
			return read;
		}
		// end of stream reached
		return 0;
	}
}

//-----------------------------------------------------------------------------------
// UTF8Buffer
//-----------------------------------------------------------------------------------
public class UTF8Buffer: Buffer {
	public UTF8Buffer(Buffer b): base(b) {}

	public override int Read() {
		int ch;
		do {
			ch = base.Read();
			// until we find a utf8 start (0xxxxxxx or 11xxxxxx)
		} while ((ch >= 128) && ((ch & 0xC0) != 0xC0) && (ch != EOF));
		if (ch < 128 || ch == EOF) {
			// nothing to do, first 127 chars are the same in ascii and utf8
			// 0xxxxxxx or end of file character
		} else if ((ch & 0xF0) == 0xF0) {
			// 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx
			int c1 = ch & 0x07; ch = base.Read();
			int c2 = ch & 0x3F; ch = base.Read();
			int c3 = ch & 0x3F; ch = base.Read();
			int c4 = ch & 0x3F;
			ch = (((((c1 << 6) | c2) << 6) | c3) << 6) | c4;
		} else if ((ch & 0xE0) == 0xE0) {
			// 1110xxxx 10xxxxxx 10xxxxxx
			int c1 = ch & 0x0F; ch = base.Read();
			int c2 = ch & 0x3F; ch = base.Read();
			int c3 = ch & 0x3F;
			ch = (((c1 << 6) | c2) << 6) | c3;
		} else if ((ch & 0xC0) == 0xC0) {
			// 110xxxxx 10xxxxxx
			int c1 = ch & 0x1F; ch = base.Read();
			int c2 = ch & 0x3F;
			ch = (c1 << 6) | c2;
		}
		return ch;
	}
}

//-----------------------------------------------------------------------------------
// Scanner
//-----------------------------------------------------------------------------------
public class Scanner {
	const char EOL = '\n';
	const int eofSym = 0; /* pdt */
	const int maxT = 43;
	const int noSym = 43;
	char valCh;       // current input character (for token.val)

	public Buffer buffer; // scanner buffer
	
	Token t;          // current token
	int ch;           // current input character
	int pos;          // byte position of current character
	int col;          // column number of current character
	int line;         // line number of current character
	int oldEols;      // EOLs that appeared in a comment;
	static readonly Dictionary<object, object> start; // maps first token character to start state

	Token tokens;     // list of tokens already peeked (first token is a dummy)
	Token pt;         // current peek token
	
	char[] tval = new char[128]; // text of current token
	int tlen;         // length of current token
	
	static Scanner() {
		start = new Dictionary<object,object>(128);
		for (int i = 48; i <= 57; ++i) start[i] = 19;
		for (int i = 35; i <= 35; ++i) start[i] = 20;
		for (int i = 95; i <= 95; ++i) start[i] = 20;
		for (int i = 97; i <= 122; ++i) start[i] = 20;
		for (int i = 34; i <= 34; ++i) start[i] = 21;
		for (int i = 64; i <= 64; ++i) start[i] = 1;
		start[45] = 31; 
		start[40] = 3; 
		start[41] = 4; 
		start[123] = 5; 
		start[125] = 6; 
		start[58] = 7; 
		start[44] = 8; 
		start[42] = 9; 
		start[46] = 25; 
		start[61] = 26; 
		start[60] = 27; 
		start[62] = 28; 
		start[47] = 29; 
		start[43] = 30; 
		start[Buffer.EOF] = -1;

	}
	
	public Scanner (string fileName) {
		try {
			Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			buffer = new Buffer(stream, false);
			Init();
		} catch (IOException) {
			throw new FatalError("Cannot open file " + fileName);
		}
	}
	
	public Scanner (Stream s) {
		buffer = new Buffer(s, true);
		Init();
	}
	
	void Init() {
		pos = -1; line = 1; col = 0;
		oldEols = 0;
		NextCh();
		if (ch == 0xEF) { // check optional byte order mark for UTF-8
			NextCh(); int ch1 = ch;
			NextCh(); int ch2 = ch;
			if (ch1 != 0xBB || ch2 != 0xBF) {
				throw new FatalError(String.Format("illegal byte order mark: EF {0,2:X} {1,2:X}", ch1, ch2));
			}
			buffer = new UTF8Buffer(buffer); col = 0;
			NextCh();
		}
		pt = tokens = new Token();  // first token is a dummy
	}
	
	void NextCh() {
		if (oldEols > 0) { ch = EOL; oldEols--; } 
		else {
			pos = buffer.Pos;
			ch = buffer.Read(); col++;
			// replace isolated '\r' by '\n' in order to make
			// eol handling uniform across Windows, Unix and Mac
			if (ch == '\r' && buffer.Peek() != '\n') ch = EOL;
			if (ch == EOL) { line++; col = 0; }
		}
		if (ch != Buffer.EOF) {
			valCh = (char) ch;
			ch = char.ToLower((char) ch);
		}

	}

	void AddCh() {
		if (tlen >= tval.Length) {
			char[] newBuf = new char[2 * tval.Length];
			Array.Copy(tval, 0, newBuf, 0, tval.Length);
			tval = newBuf;
		}
		if (ch != Buffer.EOF) {
			tval[tlen++] = valCh;
			NextCh();
		}
	}




	void CheckLiteral() {
		switch (t.val.ToLower()) {
			case "not": t.kind = 6; break;
			case "fn": t.kind = 11; break;
			case "to": t.kind = 12; break;
			case "and": t.kind = 13; break;
			case "or": t.kind = 14; break;
			case "get": t.kind = 15; break;
			case "where": t.kind = 16; break;
			case "ordered": t.kind = 19; break;
			case "by": t.kind = 20; break;
			case "desc": t.kind = 21; break;
			case "limit": t.kind = 22; break;
			case "over": t.kind = 23; break;
			case "top": t.kind = 24; break;
			case "in": t.kind = 25; break;
			case "bottom": t.kind = 26; break;
			case "via": t.kind = 27; break;
			case "as": t.kind = 28; break;
			case "select": t.kind = 29; break;
			case "group": t.kind = 30; break;
			case "from": t.kind = 32; break;
			case "having": t.kind = 33; break;
			case "like": t.kind = 34; break;
			default: break;
		}
	}

	Token NextToken() {
		while (ch == ' ' ||
			ch == 10 || ch == 13
		) NextCh();

		int recKind = noSym;
		int recEnd = pos;
		t = new Token();
		t.pos = pos; t.col = col; t.line = line; 
		int state;
		if (start.ContainsKey(ch)) { state = (int) start[ch]; }
		else { state = 0; }
		tlen = 0; AddCh();
		
		switch (state) {
			case -1: { t.kind = eofSym; break; } // NextCh already done
			case 0: {
				if (recKind != noSym) {
					tlen = recEnd - t.pos;
					SetScannerBehindT();
				}
				t.kind = recKind; break;
			} // NextCh already done
			case 1:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 2;}
				else {goto case 0;}
			case 2:
				recEnd = pos; recKind = 5;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 2;}
				else {t.kind = 5; break;}
			case 3:
				{t.kind = 7; break;}
			case 4:
				{t.kind = 8; break;}
			case 5:
				{t.kind = 9; break;}
			case 6:
				{t.kind = 10; break;}
			case 7:
				{t.kind = 17; break;}
			case 8:
				{t.kind = 18; break;}
			case 9:
				{t.kind = 31; break;}
			case 10:
				if (ch == '.') {AddCh(); goto case 11;}
				else {goto case 0;}
			case 11:
				if (ch == '"') {AddCh(); goto case 12;}
				else if (ch == '#' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 13;}
				else if (ch >= '0' && ch <= '9') {AddCh(); goto case 15;}
				else if (ch == '-') {AddCh(); goto case 14;}
				else {goto case 0;}
			case 12:
				if (ch == '"') {AddCh(); goto case 18;}
				else if (ch >= ' ' && ch <= '!' || ch >= '#' && ch <= '+' || ch >= '-' && ch <= ':' || ch == '@' || ch >= '[' && ch <= '_' || ch >= 'a' && ch <= '{' || ch == '}') {AddCh(); goto case 12;}
				else {goto case 0;}
			case 13:
				recEnd = pos; recKind = 35;
				if (ch == '#' || ch >= '0' && ch <= '9' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 13;}
				else {t.kind = 35; break;}
			case 14:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 15;}
				else {goto case 0;}
			case 15:
				recEnd = pos; recKind = 35;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 15;}
				else if (ch == '.') {AddCh(); goto case 16;}
				else {t.kind = 35; break;}
			case 16:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 17;}
				else {goto case 0;}
			case 17:
				recEnd = pos; recKind = 35;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 17;}
				else {t.kind = 35; break;}
			case 18:
				{t.kind = 35; break;}
			case 19:
				recEnd = pos; recKind = 1;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 19;}
				else if (ch == '.') {AddCh(); goto case 22;}
				else {t.kind = 1; break;}
			case 20:
				recEnd = pos; recKind = 3;
				if (ch == '#' || ch >= '0' && ch <= '9' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 20;}
				else if (ch == '.') {AddCh(); goto case 10;}
				else {t.kind = 3; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 21:
				if (ch == '"') {AddCh(); goto case 23;}
				else if (ch >= ' ' && ch <= '!' || ch >= '#' && ch <= '+' || ch >= '-' && ch <= ':' || ch == '@' || ch >= '[' && ch <= '_' || ch >= 'a' && ch <= '{' || ch == '}') {AddCh(); goto case 21;}
				else {goto case 0;}
			case 22:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 24;}
				else if (ch == '.') {AddCh(); goto case 11;}
				else {goto case 0;}
			case 23:
				recEnd = pos; recKind = 4;
				if (ch == '.') {AddCh(); goto case 10;}
				else {t.kind = 4; break;}
			case 24:
				recEnd = pos; recKind = 2;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 24;}
				else if (ch == '.') {AddCh(); goto case 10;}
				else {t.kind = 2; break;}
			case 25:
				{t.kind = 37; break;}
			case 26:
				{t.kind = 38; break;}
			case 27:
				{t.kind = 39; break;}
			case 28:
				{t.kind = 40; break;}
			case 29:
				{t.kind = 41; break;}
			case 30:
				{t.kind = 42; break;}
			case 31:
				recEnd = pos; recKind = 36;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 19;}
				else {t.kind = 36; break;}

		}
		t.val = new String(tval, 0, tlen);
		return t;
	}
	
	private void SetScannerBehindT() {
		buffer.Pos = t.pos;
		NextCh();
		line = t.line; col = t.col;
		for (int i = 0; i < tlen; i++) NextCh();
	}
	
	// get the next token (possibly a token already seen during peeking)
	public Token Scan () {
		if (tokens.next == null) {
			return NextToken();
		} else {
			pt = tokens = tokens.next;
			return tokens;
		}
	}

	// peek for the next token, ignore pragmas
	public Token Peek () {
		do {
			if (pt.next == null) {
				pt.next = NextToken();
			}
			pt = pt.next;
		} while (pt.kind > maxT); // skip pragmas
	
		return pt;
	}

	// make sure that peeking starts at the current scan position
	public void ResetPeek () { pt = tokens; }

} // end Scanner
}


namespace Bermuda.ExpressionGeneration {



using System;

public class Parser {
	public const int _EOF = 0;
	public const int _Number = 1;
	public const int _Float = 2;
	public const int _Word = 3;
	public const int _Phrase = 4;
	public const int _Id = 5;
	public const int _Not = 6;
	public const int _OpenGroup = 7;
	public const int _CloseGroup = 8;
	public const int _OpenBrace = 9;
	public const int _CloseBrace = 10;
	public const int _Fn = 11;
	public const int _RangeSeparator = 12;
	public const int _And = 13;
	public const int _Or = 14;
	public const int _Get = 15;
	public const int _Where = 16;
	public const int _Colon = 17;
	public const int _Comma = 18;
	public const int _Order = 19;
	public const int _By = 20;
	public const int _Desc = 21;
	public const int _Limit = 22;
	public const int _Over = 23;
	public const int _Top = 24;
	public const int _In = 25;
	public const int _Bottom = 26;
	public const int _Via = 27;
	public const int _As = 28;
	public const int _Select = 29;
	public const int _Group = 30;
	public const int _Star = 31;
	public const int _From = 32;
	public const int _Having = 33;
	public const int _Like = 34;
	public const int _Range = 35;
	public const int maxT = 43;

	const bool T = true;
	const bool x = false;
	const int minErrDist = 2;
	
	public Scanner scanner;
	public Errors  errors;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = minErrDist;

public RootExpression RootTree { get; private set; } 

bool FollowedByColon()  
{ 
	Token x = la; 
	//while (x.kind == _Word || x.kind == _Phrase) 
		x = scanner.Peek(); 
	return x.val == ":" || x.val == "<" || x.val == ">" || x.val == "=" || x.val.ToUpper() == "LIKE"; 
}

private void MultiAdd(ExpressionTreeBase parent, ExpressionTreeBase child)
{
	if (parent is MultiNodeTree)
	{
		((MultiNodeTree)parent).AddChild(child);
	}
	else if (parent is ConditionalExpression && child is ConditionalExpression)
	{
		((ConditionalExpression)parent).AddCondition((ConditionalExpression)child);
	}
	else if (parent is SingleNodeTree)
	{
		((SingleNodeTree)parent).SetChild(child);  
	}
}



	public Parser(Scanner scanner) {
		this.scanner = scanner;
		errors = new Errors();
	}

	void SynErr (int n) {
		if (errDist >= minErrDist) errors.SynErr(la.line, la.col, n);
		errDist = 0;
	}

	public void SemErr (string msg) {
		if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
		errDist = 0;
	}
	
	void Get () {
		for (;;) {
			t = la;
			la = scanner.Scan();
			if (la.kind <= maxT) { ++errDist; break; }

			la = t;
		}
	}
	
	void Expect (int n) {
		if (la.kind==n) Get(); else { SynErr(n); }
	}
	
	bool StartOf (int s) {
		return set[s, la.kind];
	}
	
	void ExpectWeak (int n, int follow) {
		if (la.kind == n) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}


	bool WeakSeparator(int n, int syFol, int repFol) {
		int kind = la.kind;
		if (kind == n) {Get(); return true;}
		else if (StartOf(repFol)) {return false;}
		else {
			SynErr(n);
			while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
				Get();
				kind = la.kind;
			}
			return StartOf(syFol);
		}
	}

	
	void EvoQL() {
		if (StartOf(1)) {
			GetExpression expression = new GetExpression(); RootTree = expression; 
			EqlClause(expression);
		} else if (StartOf(2)) {
			GetExpression expression = new GetExpression(); RootTree = expression; 
			QlClause(expression);
		} else if (StartOf(3)) {
			GetExpression expression = new GetExpression(); RootTree = expression; 
			ConditionGroup conditions = new ConditionGroup(); RootTree.SetChild(conditions); 
			
			Unary(conditions, false);
			while (StartOf(4)) {
				Unary(conditions, false);
			}
		} else SynErr(44);
	}

	void EqlClause(GetExpression expression) {
		if (la.kind == 15) {
			GetClause(expression);
		}
		if (StartOf(5)) {
			OptionalWhereClause(expression);
		}
	}

	void QlClause(GetExpression expression) {
		if (la.kind == 29) {
			SelectClause(expression);
		}
		if (la.kind == 32) {
			FromClause(expression);
		}
		if (la.kind == 16) {
			WhereClause(expression);
		}
		if (la.kind == 19) {
			OrderClause(expression);
		}
	}

	void Unary(MultiNodeTree parent, bool? isAsOptional) {
		ExpressionTreeBase addTo = parent; SingleNodeTree condition = null; ConditionalExpression lastOperation = null; 
		while (StartOf(4)) {
			lastOperation = lastOperation ?? new AndCondition();
			MultiAdd(addTo, lastOperation);
			addTo = lastOperation;
			
			if (la.kind == 6 || la.kind == 36) {
				if (la.kind == 6) {
					Get();
				} else {
					Get();
				}
				if (la.kind == 17) {
					Get();
				}
				NotCondition not = new NotCondition(); lastOperation.SetChild(not); lastOperation = not; 
			}
			if (StartOf(6)) {
				Condition(lastOperation, null, isAsOptional);
			} else if (la.kind == 7) {
				ConditionGroup(lastOperation, isAsOptional);
			} else SynErr(45);
			if (la.kind == 13 || la.kind == 14) {
				Operation(out lastOperation);
			}
			else { lastOperation = null; } 
		}
		if (lastOperation != null && lastOperation.Child == null) SemErr("Invalid Condition"); 
	}

	void GetClause(GetExpression expression) {
		Expect(15);
		Expect(3);
		expression.AddGet(t.val); 
		while (la.kind == 18) {
			Get();
			Expect(3);
			expression.AddGet(t.val); 
		}
	}

	void OptionalWhereClause(SingleNodeTree tree) {
		var conditions = new ConditionGroup(); tree.SetChild(conditions); 
		if (la.kind == 16) {
			Get();
		}
		Unary(conditions, false);
		while (StartOf(4)) {
			Unary(conditions, false);
		}
	}

	void SelectClause(GetExpression expression) {
		Expect(29);
		if (la.kind == 24) {
			Get();
			Expect(1);
			expression.Take = Int32.Parse(t.val); 
		}
		if (StartOf(7)) {
			if (StartOf(4)) {
				var argList = new ArgumentListExpression(); argList.SetParent(expression); 
				SelectField(argList);
				while (la.kind == 18) {
					Get();
					SelectField(argList);
				}
				expression.AddSelects(argList.GetChildren()); 
			}
			if (la.kind == 32) {
				FromClause(expression);
			}
			if (la.kind == 30) {
				GroupByClause(expression);
			}
		}
	}

	void FromClause(GetExpression expression) {
		Expect(32);
		if (la.kind == 7) {
			GetExpression subselect = new GetExpression(); expression.Subselect = subselect; 
			Get();
			QlClause(subselect);
			Expect(8);
		} else if (la.kind == 3 || la.kind == 4 || la.kind == 31) {
			var identifier = new IdentifierExpression(); string alias = null; 
			Identifier(identifier);
			if (la.kind == 3 || la.kind == 4 || la.kind == 28) {
				if (la.kind == 28) {
					Get();
				}
				AliasClause(ref alias);
			}
			expression.AddCollection(identifier.LastPart, alias); 
			while (la.kind == 18) {
				Get();
				var identifier2 = new IdentifierExpression(); string alias2 = null; 
				Identifier(identifier);
				if (la.kind == 3 || la.kind == 4 || la.kind == 28) {
					if (la.kind == 28) {
						Get();
					}
					AliasClause(ref alias);
				}
				expression.AddCollection(identifier2.LastPart, alias2); 
			}
		} else SynErr(46);
	}

	void WhereClause(SingleNodeTree tree) {
		var conditions = new ConditionGroup(); tree.SetChild(conditions); 
		Expect(16);
		Unary(conditions, false);
		while (StartOf(4)) {
			Unary(conditions, false);
		}
	}

	void OrderClause(GetExpression expression) {
		Expect(19);
		Expect(20);
		var ordering = new DimensionExpression(); expression.Ordering = ordering; 
		Primary(ordering, true);
		if (la.kind == 21) {
			Get();
			expression.OrderDescending = true; 
		}
		if (la.kind == 22) {
			Get();
			Expect(1);
			expression.Take = Int32.Parse(t.val); 
			if (la.kind == 18) {
				Get();
				Expect(1);
				expression.Skip = expression.Take; expression.Take = Int32.Parse(t.val); 
			}
		}
	}

	void Primary(DimensionExpression sel, bool? isAsOptional) {
		if (la.kind == 3 || la.kind == 4 || la.kind == 31) {
			var identifier = new IdentifierExpression(); 
			Identifier(identifier);
			sel.Source = identifier.LastPart; sel.SourceType = typeof(string); 
            sel.IsQuoted = identifier.IsQuoted; 
			if (la.kind == 7) {
				sel.Function = sel.Source; sel.Source = null; 
				Get();
				if (StartOf(4)) {
					var argList = new ArgumentListExpression(); argList.SetParent(sel); sel.IsFunctionCall = true; 
					SelectField(argList);
					while (la.kind == 18) {
						Get();
						SelectField(argList);
					}
					sel.AddArguments(argList.GetChildren()); 
				}
				Expect(8);
			}
		} else if (la.kind == 9) {
			Get();
			Expect(11);
			Primary(sel, null);
			Expect(10);
		} else if (la.kind == 1) {
			Get();
			sel.Source = t.val; sel.SourceType = typeof(long); 
		} else if (la.kind == 2) {
			Get();
			sel.Source = t.val; sel.SourceType = typeof(double); 
		} else if (la.kind == 4) {
			Get();
			sel.Source = t.val.Substring(1, t.val.Length - 2); sel.SourceType = typeof(string); sel.IsQuoted = true; 
		} else SynErr(47);
	}

	void SelectField(ArgumentListExpression expression, DimensionExpression dimensionToUse = null) {
		var selLol = dimensionToUse ?? new DimensionExpression(); 
		  if(expression != null) 
		{
		selLol.SetParent(expression);  
		expression.AddArgument(selLol); 
		}
		
		if (la.kind == 6 || la.kind == 36) {
			if (la.kind == 6) {
				Get();
				selLol.IsNotted = true; 
			} else {
				Get();
				selLol.IsNegated = true; 
			}
			if (la.kind == 17) {
				Get();
			}
		}
		Condition(selLol, null, true);
		var selectorChild = selLol.Child as SelectorExpression;
		  if (expression != null && selectorChild != null)
		  {
		      var selectorChildRightDimension = selectorChild.Right as DimensionExpression;
		      if (selectorChildRightDimension != null && selectorChild.Left == null)
		      {
		          selectorChildRightDimension.Target = selectorChild.Target;
			selectorChildRightDimension.IsNegated = selLol.IsNegated;
		          selectorChildRightDimension.IsNotted = selLol.IsNotted;
		          expression.AddArgument(selectorChildRightDimension);
		          expression.RemoveArgument(selLol);
		      }
		  }
		
	}

	void GroupByClause(GetExpression expression) {
		Expect(30);
		Expect(20);
		var groupByDimension = new DimensionExpression(); 
		expression.AddDimension(groupByDimension); 
		
		GroupByField(groupByDimension);
		while (la.kind == 18) {
			Get();
			var groupByDimension2 = new DimensionExpression(); 
			expression.AddDimension(groupByDimension2); 
			
			GroupByField(groupByDimension2);
		}
		if (la.kind == 33) {
			HavingClause(expression);
		}
	}

	void Condition(SingleNodeTree parent, SelectorExpression parentSelector, bool? isAsOptional) {
		SelectorTypes selectorType = SelectorTypes.Unspecified; 
		ModifierTypes modifierType; 
		DimensionExpression dim; 
		ModifierTypes modifierResult; 
		
		SelectorExpression selector = new SelectorExpression(); MultiAdd(parent, selector); 
		if(parentSelector != null) 
		{
		selector.SetNodeType(parentSelector.NodeType);
		selector.SetModifierType(parentSelector.Modifier);
		selector.SetLeft(parentSelector.Left);
		}
		
		if (la.kind == 7) {
			ComplexCondition(parent, selector, false);
		} else if (StartOf(8)) {
			dim = new DimensionExpression(); selector.SetRight(dim); 
			Primary(dim, false);
		} else if (StartOf(9)) {
			ExpressionTreeBase expr = null;  
			LiteralExpression(ref expr);
			selector.SetRight(expr); 
		} else SynErr(48);
		if (StartOf(10)) {
			Modifier(out modifierResult);
			selector.SetLeft(selector.Right); selector.SetRight(null); modifierType = modifierResult; selector.SetModifierType(modifierType); selector.SetNodeType(SelectorTypes.Unknown); 
			if (la.kind == 7) {
				ComplexCondition(parent, selector, false);
			} else if (StartOf(8)) {
				dim = new DimensionExpression(); selector.SetRight(dim); 
				Primary(dim, false);
			} else if (StartOf(9)) {
				ExpressionTreeBase expr = null;  
				LiteralExpression(ref expr);
				selector.SetRight(expr); 
			} else SynErr(49);
		}
		if(isAsOptional == true){	 
		if (la.kind == 3 || la.kind == 4 || la.kind == 28) {
			if (la.kind == 28) {
				Get();
			}
			string alias = null; 
			AliasClause(ref alias);
			selector.Target = alias; 
		}
		}else if(isAsOptional == false){ 
		if (la.kind == 28) {
			Get();
			string alias = null; 
			AliasClause(ref alias);
			selector.Target = alias; 
		}
		} 
	}

	void GroupByField(DimensionExpression expression) {
		if (la.kind == 24 || la.kind == 26) {
			if (la.kind == 24) {
				Get();
				expression.OrderDescending = true; 
			} else {
				Get();
				expression.OrderDescending = false; 
			}
			Expect(1);
			expression.Take = Int32.Parse(t.val); 
		}
		SelectField(null, expression);
		if (la.kind == 25) {
			var inClause = new InExpression(); expression.InClause = inClause; 
			InClause(inClause);
		}
		if (la.kind == 27) {
			Get();
			var dim = new DimensionExpression(); 
			Primary(dim, false);
			expression.Ordering = dim; 
		}
	}

	void HavingClause(GetExpression expression) {
		Expect(33);
		var having = new HavingExpression();
		ConditionGroup conditions = new ConditionGroup(); 
		expression.SetHaving(having); 
		having.SetChild(conditions); 
		
		Unary(conditions, false);
		while (StartOf(4)) {
			Unary(conditions, false);
		}
	}

	void Identifier(IdentifierExpression expression) {
		if (la.kind == 3) {
			Get();
			expression.Parts.Add(t.val); 
		} else if (la.kind == 4) {
			Get();
			expression.Parts.Add(t.val.Substring(1, t.val.Length-2)); expression.IsQuoted=true; 
		} else if (la.kind == 31) {
			Get();
			expression.Parts.Add(t.val); 
		} else SynErr(50);
		while (la.kind == 37) {
			Get();
			if (la.kind == 3) {
				Get();
				expression.Parts.Add(t.val); 
			} else if (la.kind == 4) {
				Get();
				expression.Parts.Add(t.val.Substring(1, t.val.Length-2)); expression.IsQuoted=false; 
			} else if (la.kind == 31) {
				Get();
				expression.Parts.Add(t.val); 
			} else SynErr(51);
		}
	}

	void AliasClause(ref string alias) {
		if (la.kind == 3) {
			Get();
			alias = t.val; 
		} else if (la.kind == 4) {
			Get();
			alias = t.val.Substring(1, t.val.Length-2); 
		} else SynErr(52);
	}

	void InClause(InExpression inExpression) {
		Expect(25);
		Expect(7);
		ExpressionTreeBase lit = null; 
		LiteralExpression(ref lit);
		inExpression.AddItem(lit); 
		while (la.kind == 18) {
			Get();
			ExpressionTreeBase lit2 = null; 
			LiteralExpression(ref lit2);
			inExpression.AddItem(lit2); 
		}
		Expect(8);
	}

	void LiteralExpression(ref ExpressionTreeBase expr) {
		if (la.kind == 35) {
			Get();
			expr = new RangeExpression(t.val); 
		} else if (la.kind == 3) {
			Get();
			expr = new LiteralExpression(t.val); 
		} else if (la.kind == 4) {
			Get();
			expr = new LiteralExpression(t.val.Substring(1, t.val.Length - 2), true); 
		} else if (la.kind == 5) {
			Get();
			expr = new ValueExpression(Int32.Parse(t.val.Substring(1))); 
		} else if (la.kind == 1) {
			Get();
			expr = new LiteralExpression(t.val); 
		} else SynErr(53);
	}

	void ConditionGroup(SingleNodeTree parent, bool? isAsOptional) {
		ConditionGroup group = new ConditionGroup(); parent.SetChild(group); 
		ExpressionTreeBase addTo = group; SingleNodeTree condition = null; 
		ConditionalExpression lastOperation = null; 
		Expect(7);
		while (StartOf(4)) {
			lastOperation = lastOperation ?? new AndCondition();
			MultiAdd(addTo, lastOperation);
			addTo = lastOperation;
			
			if (la.kind == 6 || la.kind == 36) {
				if (la.kind == 6) {
					Get();
				} else {
					Get();
				}
				if (la.kind == 17) {
					Get();
				}
				NotCondition not = new NotCondition(); lastOperation.SetChild(not); lastOperation = not; 
			}
			if (StartOf(6)) {
				Condition(lastOperation, null, isAsOptional);
			} else if (la.kind == 7) {
				ConditionGroup(lastOperation, isAsOptional);
			} else SynErr(54);
			if (la.kind == 13 || la.kind == 14) {
				Operation(out lastOperation);
			}
			else { lastOperation = null; } 
		}
		if (lastOperation != null && lastOperation.Child == null) SemErr("Invalid Condition"); 
		Expect(8);
	}

	void Operation(out ConditionalExpression expression) {
		expression = null; 
		if (la.kind == 13) {
			Get();
			expression = new AndCondition(); 
		} else if (la.kind == 14) {
			Get();
			expression = new OrCondition(); 
		} else SynErr(55);
	}

	void ComplexCondition(SingleNodeTree parent, SelectorExpression selector, bool? isAsOptional) {
		ConditionGroup group = new ConditionGroup(); parent.SetChild(group); ExpressionTreeBase addTo = group; SingleNodeTree condition = null; ConditionalExpression lastOperation = null; 
		Expect(7);
		while (StartOf(4)) {
			lastOperation = lastOperation ?? new AndCondition();
			MultiAdd(addTo, lastOperation);
			addTo = lastOperation;
			var nestedselector = new SelectorExpression(selector.NodeType, selector.Modifier, selector.Left);
			
			if (la.kind == 6 || la.kind == 36) {
				if (la.kind == 6) {
					Get();
				} else {
					Get();
				}
				if (la.kind == 17) {
					Get();
				}
				NotCondition not = new NotCondition(); lastOperation.SetChild(not); lastOperation = not; 
			}
			if (la.kind == 7) {
				ComplexCondition(lastOperation, nestedselector, isAsOptional);
			} else if (StartOf(6)) {
				Condition(lastOperation, nestedselector, isAsOptional);
			} else SynErr(56);
			if (la.kind == 13 || la.kind == 14) {
				Operation(out lastOperation);
			}
			else { lastOperation = null; } 
		}
		Expect(8);
	}

	void Modifier(out ModifierTypes type) {
		type = ModifierTypes.Equals; 
		switch (la.kind) {
		case 17: {
			Get();
			type = ModifierTypes.Colon; 
			break;
		}
		case 38: {
			Get();
			type = ModifierTypes.Equals; 
			break;
		}
		case 34: {
			Get();
			type = ModifierTypes.Like; 
			break;
		}
		case 39: {
			Get();
			type = ModifierTypes.LessThan; 
			break;
		}
		case 40: {
			Get();
			type = ModifierTypes.GreaterThan; 
			break;
		}
		case 31: {
			Get();
			type = ModifierTypes.Multiply; 
			break;
		}
		case 41: {
			Get();
			type = ModifierTypes.Divide; 
			break;
		}
		case 42: {
			Get();
			type = ModifierTypes.Add; 
			break;
		}
		case 36: {
			Get();
			type = ModifierTypes.Subtract; 
			break;
		}
		default: SynErr(57); break;
		}
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		EvoQL();
		Expect(0);

    Expect(0);
	}
	
	static readonly bool[,] set = {
		{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{T,T,T,T, T,T,T,T, x,T,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,T, T,x,x,x, x,x,x,x, x},
		{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, x,T,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x},
		{T,T,T,T, T,T,T,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,T, T,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,T,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,T, T,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,T,T, x,T,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,T, T,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,T,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,x,x,T, T,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,x,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,T,x, T,x,T,T, T,T,T,x, x}

	};
} // end Parser


public class Errors {
	public int count = 0;                                    // number of errors detected
	public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
  public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text
  
	public void SynErr (int line, int col, int n) {
		string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "Number expected"; break;
			case 2: s = "Float expected"; break;
			case 3: s = "Word expected"; break;
			case 4: s = "Phrase expected"; break;
			case 5: s = "Id expected"; break;
			case 6: s = "Not expected"; break;
			case 7: s = "OpenGroup expected"; break;
			case 8: s = "CloseGroup expected"; break;
			case 9: s = "OpenBrace expected"; break;
			case 10: s = "CloseBrace expected"; break;
			case 11: s = "Fn expected"; break;
			case 12: s = "RangeSeparator expected"; break;
			case 13: s = "And expected"; break;
			case 14: s = "Or expected"; break;
			case 15: s = "Get expected"; break;
			case 16: s = "Where expected"; break;
			case 17: s = "Colon expected"; break;
			case 18: s = "Comma expected"; break;
			case 19: s = "Order expected"; break;
			case 20: s = "By expected"; break;
			case 21: s = "Desc expected"; break;
			case 22: s = "Limit expected"; break;
			case 23: s = "Over expected"; break;
			case 24: s = "Top expected"; break;
			case 25: s = "In expected"; break;
			case 26: s = "Bottom expected"; break;
			case 27: s = "Via expected"; break;
			case 28: s = "As expected"; break;
			case 29: s = "Select expected"; break;
			case 30: s = "Group expected"; break;
			case 31: s = "Star expected"; break;
			case 32: s = "From expected"; break;
			case 33: s = "Having expected"; break;
			case 34: s = "Like expected"; break;
			case 35: s = "Range expected"; break;
			case 36: s = "\"-\" expected"; break;
			case 37: s = "\".\" expected"; break;
			case 38: s = "\"=\" expected"; break;
			case 39: s = "\"<\" expected"; break;
			case 40: s = "\">\" expected"; break;
			case 41: s = "\"/\" expected"; break;
			case 42: s = "\"+\" expected"; break;
			case 43: s = "??? expected"; break;
			case 44: s = "invalid EvoQL"; break;
			case 45: s = "invalid Unary"; break;
			case 46: s = "invalid FromClause"; break;
			case 47: s = "invalid Primary"; break;
			case 48: s = "invalid Condition"; break;
			case 49: s = "invalid Condition"; break;
			case 50: s = "invalid Identifier"; break;
			case 51: s = "invalid Identifier"; break;
			case 52: s = "invalid AliasClause"; break;
			case 53: s = "invalid LiteralExpression"; break;
			case 54: s = "invalid ConditionGroup"; break;
			case 55: s = "invalid Operation"; break;
			case 56: s = "invalid ComplexCondition"; break;
			case 57: s = "invalid Modifier"; break;

			default: s = "error " + n; break;
		}
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}

	public void SemErr (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}
	
	public void SemErr (string s) {
		errorStream.WriteLine(s);
		count++;
	}
	
	public void Warning (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
	}
	
	public void Warning(string s) {
		errorStream.WriteLine(s);
	}
} // Errors


public class FatalError: Exception {
	public FatalError(string m): base(m) {}
}
}