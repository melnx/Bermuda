namespace Bermuda.Core.QL.Sql {

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
	const int maxT = 40;
	const int noSym = 40;
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
		for (int i = 48; i <= 57; ++i) start[i] = 2;
		for (int i = 46; i <= 46; ++i) start[i] = 3;
		for (int i = 97; i <= 122; ++i) start[i] = 3;
		for (int i = 34; i <= 34; ++i) start[i] = 4;
		for (int i = 64; i <= 64; ++i) start[i] = 6;
		start[45] = 1; 
		start[40] = 8; 
		start[41] = 9; 
		start[58] = 10; 
		start[44] = 11; 
		start[42] = 12; 
		start[61] = 13; 
		start[60] = 14; 
		start[62] = 15; 
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
			case "not": t.kind = 5; break;
			case "to": t.kind = 8; break;
			case "and": t.kind = 9; break;
			case "or": t.kind = 10; break;
			case "get": t.kind = 11; break;
			case "set": t.kind = 12; break;
			case "chart": t.kind = 13; break;
			case "where": t.kind = 14; break;
			case "ordered": t.kind = 17; break;
			case "interval": t.kind = 18; break;
			case "by": t.kind = 19; break;
			case "desc": t.kind = 20; break;
			case "limit": t.kind = 21; break;
			case "over": t.kind = 22; break;
			case "top": t.kind = 23; break;
			case "bottom": t.kind = 24; break;
			case "via": t.kind = 25; break;
			case "as": t.kind = 26; break;
			case "from": t.kind = 27; break;
			case "select": t.kind = 28; break;
			case "on": t.kind = 30; break;
			case "group": t.kind = 31; break;
			case "inner": t.kind = 32; break;
			case "outer": t.kind = 33; break;
			case "left": t.kind = 34; break;
			case "right": t.kind = 35; break;
			case "join": t.kind = 36; break;
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
				recEnd = pos; recKind = 1;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 2;}
				else {t.kind = 1; break;}
			case 3:
				recEnd = pos; recKind = 2;
				if (ch == '#' || ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 3;}
				else {t.kind = 2; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 4:
				if (ch == '"') {AddCh(); goto case 5;}
				else if (ch >= ' ' && ch <= '!' || ch >= '#' && ch <= '+' || ch >= '-' && ch <= ':' || ch == '@' || ch >= '[' && ch <= '_' || ch >= 'a' && ch <= '{' || ch == '}') {AddCh(); goto case 4;}
				else {goto case 0;}
			case 5:
				{t.kind = 3; break;}
			case 6:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 7;}
				else {goto case 0;}
			case 7:
				recEnd = pos; recKind = 4;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 7;}
				else {t.kind = 4; break;}
			case 8:
				{t.kind = 6; break;}
			case 9:
				{t.kind = 7; break;}
			case 10:
				{t.kind = 15; break;}
			case 11:
				{t.kind = 16; break;}
			case 12:
				{t.kind = 29; break;}
			case 13:
				{t.kind = 37; break;}
			case 14:
				{t.kind = 38; break;}
			case 15:
				{t.kind = 39; break;}

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


namespace Bermuda.Core.QL.Sql {



using System;

public class Parser {
	public const int _EOF = 0;
	public const int _Number = 1;
	public const int _Word = 2;
	public const int _Phrase = 3;
	public const int _Id = 4;
	public const int _Not = 5;
	public const int _OpenGroup = 6;
	public const int _CloseGroup = 7;
	public const int _RangeSeparator = 8;
	public const int _And = 9;
	public const int _Or = 10;
	public const int _Get = 11;
	public const int _Set = 12;
	public const int _Chart = 13;
	public const int _Where = 14;
	public const int _Colon = 15;
	public const int _Comma = 16;
	public const int _Order = 17;
	public const int _Interval = 18;
	public const int _By = 19;
	public const int _Desc = 20;
	public const int _Limit = 21;
	public const int _Over = 22;
	public const int _Top = 23;
	public const int _Bottom = 24;
	public const int _Via = 25;
	public const int _As = 26;
	public const int _From = 27;
	public const int _Select = 28;
	public const int _Star = 29;
	public const int _On = 30;
	public const int _Group = 31;
	public const int _Inner = 32;
	public const int _Outer = 33;
	public const int _Left = 34;
	public const int _Right = 35;
	public const int _Join = 36;
	public const int _Equals = 37;
	public const int maxT = 40;

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
	return x.val == ":" || x.val == "<" || x.val == ">"; 
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

	
	void EvoSQL() {
		var expression = new SelectExpression(); RootTree = expression; 
		SelectBlock(expression);
	}

	void SelectBlock(SelectExpression expression) {
		Expect(28);
		if (la.kind == 2) {
			SelectPart(expression);
			while (la.kind == 16) {
				Get();
				SelectPart(expression);
			}
		} else if (la.kind == 29) {
			Get();
			var sel = new ExpressionGeneration.SelectDescriptor(); sel.Star = true; 
		} else SynErr(41);
		Expect(27);
		if (la.kind == 2) {
			FromSource(expression);
		} else if (la.kind == 28) {
			var subexpression = new SelectExpression(); 
			SelectBlock(subexpression);
			expression.SetFrom(subexpression); 
		} else SynErr(42);
		if (la.kind == 14 || la.kind == 17 || la.kind == 31) {
			OrderLimitClause(expression);
		}
		if (la.kind == 14) {
			Get();
			ConditionGroup conditions = new ConditionGroup(); expression.SetChild(conditions); 
			Conditional(conditions);
			while (StartOf(1)) {
				Conditional(conditions);
			}
		}
		if (la.kind == 17 || la.kind == 31) {
			OrderLimitClause(expression);
		}
		if (la.kind == 31) {
			Get();
			Expect(19);
			Expect(2);
			expression.AddDimension(t.val); 
			while (la.kind == 16) {
				Get();
				Expect(2);
				expression.AddDimension(t.val); 
			}
			OrderLimitClause(expression);
		}
	}

	void SelectPart(GetExpression expression) {
		Expect(2);
		var sel = new SelectDimensionExpression(); expression.AddSelect(sel); sel.Target = t.val; sel.Source = t.val; 
		if (la.kind == 6) {
			Get();
			sel.Aggregate = sel.Source; sel.Source = sel.Target = null; 
			if (la.kind == 2) {
				Get();
				sel.Source = t.val; 
			}
			Expect(7);
		}
		if (la.kind == 26) {
			Get();
			Expect(2);
			sel.Target = t.val; 
		}
	}

	void FromSource(GetExpression expression) {
		Expect(2);
		var col = new ExpressionGeneration.CollectionDescriptor(); expression.AddCollection(col); col.Name = t.val; 
		if (la.kind == 2) {
			Get();
			col.Alias = t.val; 
		}
		while (la.kind == 32 || la.kind == 33) {
			if (la.kind == 32) {
				Get();
			} else {
				Get();
			}
			if (la.kind == 34 || la.kind == 35) {
				if (la.kind == 34) {
					Get();
				} else {
					Get();
				}
			}
			Expect(36);
			Expect(2);
			var col2 = new ExpressionGeneration.CollectionDescriptor(); expression.AddCollection(col); col2.Name = t.val; 
			if (la.kind == 2) {
				Get();
				col2.Alias = t.val;  
			}
			Expect(30);
			Expect(2);
			Expect(37);
			Expect(2);
		}
	}

	void OrderLimitClause(GetExpression expression) {
		if (la.kind == 17) {
			Get();
			Expect(19);
			if (la.kind == 3) {
				Get();
				expression.Ordering = t.val.Substring(1, t.val.Length - 2); 
			} else if (la.kind == 2) {
				Get();
				expression.Ordering = t.val; 
			} else SynErr(43);
			if (la.kind == 20) {
				Get();
				expression.OrderDescending = true; 
			}
			if (la.kind == 21) {
				Get();
				Expect(1);
				expression.Take = Int32.Parse(t.val); 
				if (la.kind == 16) {
					Get();
					Expect(1);
					expression.Skip = expression.Take; expression.Take = Int32.Parse(t.val); 
				}
			}
		}
	}

	void Conditional(MultiNodeTree parent) {
		ExpressionTreeBase addTo = parent; SingleNodeTree condition = null; ConditionalExpression lastOperation = null; 
		while (StartOf(2)) {
			lastOperation = lastOperation ?? new AndCondition();
			MultiAdd(addTo, lastOperation);
			addTo = lastOperation;
			
			if (la.kind == 5) {
				Get();
				NotCondition not = new NotCondition(); lastOperation.SetChild(not); lastOperation = not; 
			}
			if (StartOf(3)) {
				Condition(lastOperation);
			} else if (la.kind == 6) {
				ConditionGroup(lastOperation);
			} else SynErr(44);
			if (la.kind == 9 || la.kind == 10) {
				Operation(out lastOperation);
			}
			else { lastOperation = null; } 
		}
		if (lastOperation != null && lastOperation.Child == null) SemErr("Invalid Condition"); 
	}

	void Condition(SingleNodeTree parent) {
		SelectorTypes selectorType; ModifierTypes modifierType; 
		if (FollowedByColon()) {
			Selector(out selectorType, 
out modifierType);
			SelectorExpression selector = new SelectorExpression(selectorType, modifierType, t.val);  
			if (la.kind == 6) {
				ComplexCondition(parent, selector);
			} else if (StartOf(3)) {
				parent.SetChild(selector); 
				Literal(selector);
			} else SynErr(45);
		} else if (StartOf(3)) {
			SelectorExpression nestedSelector = new SelectorExpression(SelectorTypes.Unspecified, ModifierTypes.Equals, t.val); parent.SetChild(nestedSelector); 
			Literal(nestedSelector);
		} else SynErr(46);
	}

	void ConditionGroup(SingleNodeTree parent) {
		ConditionGroup group = new ConditionGroup(); parent.SetChild(group); ExpressionTreeBase addTo = group; SingleNodeTree condition = null; ConditionalExpression lastOperation = null; 
		Expect(6);
		while (StartOf(2)) {
			lastOperation = lastOperation ?? new AndCondition();
			MultiAdd(addTo, lastOperation);
			addTo = lastOperation;
			
			if (la.kind == 5) {
				Get();
				NotCondition not = new NotCondition(); lastOperation.SetChild(not); lastOperation = not; 
			}
			if (StartOf(3)) {
				Condition(lastOperation);
			} else if (la.kind == 6) {
				ConditionGroup(lastOperation);
			} else SynErr(47);
			if (la.kind == 9 || la.kind == 10) {
				Operation(out lastOperation);
			}
			else { lastOperation = null; } 
		}
		if (lastOperation != null && lastOperation.Child == null) SemErr("Invalid Condition"); 
		Expect(7);
	}

	void Operation(out ConditionalExpression expression) {
		expression = null; 
		if (la.kind == 9) {
			Get();
			expression = new AndCondition(); 
		} else if (la.kind == 10) {
			Get();
			expression = new OrCondition(); 
		} else SynErr(48);
	}

	void Selector(out SelectorTypes selectorType, 
out ModifierTypes modifierType) {
		SelectorTypes type; ModifierTypes modifierResult; 
		Expect(2);
		selectorType = SelectorTypes.Unknown; 
		Modifier(out modifierResult);
		modifierType = modifierResult; 
	}

	void ComplexCondition(SingleNodeTree parent, SelectorExpression selector) {
		ConditionGroup group = new ConditionGroup(); parent.SetChild(group); ExpressionTreeBase addTo = group; SingleNodeTree condition = null; ConditionalExpression lastOperation = null; 
		Expect(6);
		while (StartOf(2)) {
			lastOperation = lastOperation ?? new AndCondition();
			MultiAdd(addTo, lastOperation);
			addTo = lastOperation;
			selector = new SelectorExpression(selector.Field, ModifierTypes.Equals, selector.Path);
			
			if (la.kind == 5) {
				Get();
				NotCondition not = new NotCondition(); lastOperation.SetChild(not); lastOperation = not; 
			}
			if (la.kind == 6) {
				ComplexCondition(lastOperation, selector);
			} else if (StartOf(3)) {
				SelectorExpression nestedSelector = new SelectorExpression(selector.Field, ModifierTypes.Equals, selector.Path); MultiAdd(lastOperation, nestedSelector); 
				Literal(nestedSelector);
			} else SynErr(49);
			if (la.kind == 9 || la.kind == 10) {
				Operation(out lastOperation);
			}
			else { lastOperation = null; } 
		}
		Expect(7);
	}

	void Literal(SingleNodeTree parent) {
		if (la.kind == 2) {
			Get();
			parent.SetChild(new LiteralExpression(t.val)); 
		} else if (la.kind == 3) {
			Get();
			parent.SetChild(new LiteralExpression(t.val.Substring(1, t.val.Length - 2), true)); 
		} else if (la.kind == 4) {
			Get();
			parent.SetChild(new ValueExpression(Int32.Parse(t.val.Substring(1)))); 
		} else if (la.kind == 1) {
			Get();
			parent.SetChild(new LiteralExpression(t.val)); 
		} else SynErr(50);
	}

	void Modifier(out ModifierTypes type) {
		type = ModifierTypes.Equals; 
		if (la.kind == 37) {
			Get();
			type = ModifierTypes.Equals; 
		} else if (la.kind == 38) {
			Get();
			type = ModifierTypes.LessThan; 
		} else if (la.kind == 39) {
			Get();
			type = ModifierTypes.GreaterThan; 
		} else SynErr(51);
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		EvoSQL();
		Expect(0);

    Expect(0);
	}
	
	static readonly bool[,] set = {
		{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
		{x,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x},
		{x,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
		{x,T,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x}

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
			case 2: s = "Word expected"; break;
			case 3: s = "Phrase expected"; break;
			case 4: s = "Id expected"; break;
			case 5: s = "Not expected"; break;
			case 6: s = "OpenGroup expected"; break;
			case 7: s = "CloseGroup expected"; break;
			case 8: s = "RangeSeparator expected"; break;
			case 9: s = "And expected"; break;
			case 10: s = "Or expected"; break;
			case 11: s = "Get expected"; break;
			case 12: s = "Set expected"; break;
			case 13: s = "Chart expected"; break;
			case 14: s = "Where expected"; break;
			case 15: s = "Colon expected"; break;
			case 16: s = "Comma expected"; break;
			case 17: s = "Order expected"; break;
			case 18: s = "Interval expected"; break;
			case 19: s = "By expected"; break;
			case 20: s = "Desc expected"; break;
			case 21: s = "Limit expected"; break;
			case 22: s = "Over expected"; break;
			case 23: s = "Top expected"; break;
			case 24: s = "Bottom expected"; break;
			case 25: s = "Via expected"; break;
			case 26: s = "As expected"; break;
			case 27: s = "From expected"; break;
			case 28: s = "Select expected"; break;
			case 29: s = "Star expected"; break;
			case 30: s = "On expected"; break;
			case 31: s = "Group expected"; break;
			case 32: s = "Inner expected"; break;
			case 33: s = "Outer expected"; break;
			case 34: s = "Left expected"; break;
			case 35: s = "Right expected"; break;
			case 36: s = "Join expected"; break;
			case 37: s = "Equals expected"; break;
			case 38: s = "\"<\" expected"; break;
			case 39: s = "\">\" expected"; break;
			case 40: s = "??? expected"; break;
			case 41: s = "invalid SelectBlock"; break;
			case 42: s = "invalid SelectBlock"; break;
			case 43: s = "invalid OrderLimitClause"; break;
			case 44: s = "invalid Conditional"; break;
			case 45: s = "invalid Condition"; break;
			case 46: s = "invalid Condition"; break;
			case 47: s = "invalid ConditionGroup"; break;
			case 48: s = "invalid Operation"; break;
			case 49: s = "invalid ComplexCondition"; break;
			case 50: s = "invalid Literal"; break;
			case 51: s = "invalid Modifier"; break;

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