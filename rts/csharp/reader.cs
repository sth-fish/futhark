    private Stream s;

    // Note that the lookahead buffer does not interact well with
    // binary reading.  We are careful to not let this become a
    // problem.
    private Stack<char> LookaheadBuffer = new Stack<char>();

    void ResetLookahead(){
        LookaheadBuffer.Clear();
    }

    public void ValueReader(Stream s)
    {
        this.s = s;
    }

    public void ValueReader()
    {
        this.s = Console.OpenStandardInput();
    }

    private char? GetChar()
    {
        char c;
        if (LookaheadBuffer.Count == 0)
        {
            c = (char) this.s.ReadByte();
        }
        else
        {
            c = LookaheadBuffer.Pop();
        }

        return c;
    }

    char[] GetChars(int n)
    {
        return Enumerable.Range(0, n).Select(_ => GetChar().Value).ToArray();
    }

    void UngetChar(char c)
    {
        LookaheadBuffer.Push(c);
    }

    char PeekChar()
    {
        var c = GetChar();
        UngetChar(c.Value);
        return c.Value;
    }

    void SkipSpaces()
    {
        var c = GetChar();
        while (c.HasValue){
            if (char.IsWhiteSpace(c.Value))
            {
                c = GetChar();
            }
            else if (c == '-')
            {
                if (PeekChar() == '-')
                {
                    while (c.Value != '\n')
                    {
                        c = GetChar();
                    }
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        if (c.HasValue)
        {
            UngetChar(c.Value);
        }
    }

    bool ParseSpecificChar(char c)
    {
        var got = GetChar();
        if (got.Value != c)
        {
            UngetChar(got.Value);
            throw new Exception("ValueError");
        }
        return true;
    }

    bool ParseSpecificString(string str)
    {
        foreach (var c in str.ToCharArray())
        {
            ParseSpecificChar(c);
        }

        return true;
    }

    string Optional(Func<string> p)
    {
        string res = null;
        try
        {
            res = p();
        }
        catch (Exception)
        {
        }

        return res;
    }

    bool Optional(Func<char, bool> p, char c)
    {
        try
        {
            return p(c);
        }
        catch (Exception)
        {
        }

        return false;
    }

    bool OptionalSpecificString(string s)
    {
        var c = PeekChar();
        if (c == s[0])
        {
            return ParseSpecificString(s);
        }
        return false;
    }


    List<string> sepBy(Func<string> p, Func<string> sep)
    {
        var elems = new List<string>();
        var x = Optional(p);
        if (!string.IsNullOrWhiteSpace(x))
        {
            elems.Add(x);
            while (!string.IsNullOrWhiteSpace(Optional(sep)))
            {
                var y = Optional(p);
                elems.Add(y);
            }
        }
        return elems;
    }

    string ParseHexInt()
    {
        var s = "";
        var c = GetChar();
        while (c.HasValue)
        {
            if (Uri.IsHexDigit(c.Value))
            {
                s += c.Value;
                c = GetChar();
            }
            else if (c == '_')
            {
                c = GetChar();
            }
            else
            {
                UngetChar(c.Value);
                break;
            }
        }

        return Convert.ToString(Convert.ToUInt32(s, 16));
    }

    string ParseInt()
    {
        var s = "";
        var c = GetChar();
        if (c.Value == '0' && "xX".Contains(PeekChar()))
        {
            GetChar();
            s += ParseHexInt();
        }
        else
        {
            while (c.HasValue)
            {
                if (char.IsDigit(c.Value))
                {
                    s += c.Value;
                    c = GetChar();
                }else if (c == '_')
                {
                    c = GetChar();
                }
                else
                {
                    UngetChar(c.Value);
                    break;
                }
            }

        }

        if (s.Length == 0)
        {
            throw new Exception("ValueError");
        }

        return s;
    }

    string ParseIntSigned()
    {
        var c = GetChar();
        if (c.Value == '-' && char.IsDigit(PeekChar()))
        {
            return c + ParseInt();
        }
        else
        {
            if (c.Value != '+')
            {
                UngetChar(c.Value);
            }

            return ParseInt();
        }
    }

    string ReadStrComma()
    {
        SkipSpaces();
        ParseSpecificChar(',');
        return ",";
    }

    int ReadStrInt(string s)
    {
        SkipSpaces();
        var x = Convert.ToInt32(ParseIntSigned());
        OptionalSpecificString(s);
        return x;
    }

    int ReadStrUint(string s)
    {
        SkipSpaces();
        var x = Convert.ToInt32(ParseInt());
        OptionalSpecificString(s);
        return x;
    }

    int ReadStrI8(){return ReadStrInt("i8");}
    int ReadStrI16(){return ReadStrInt("i16");}
    int ReadStrI32(){return ReadStrInt("i32");}
    int ReadStrI64(){return ReadStrInt("i64");}
    int ReadStrU8(){return ReadStrInt("u8");}
    int ReadStrU16(){return ReadStrInt("u16");}
    int ReadStrU32(){return ReadStrInt("u32");}
    int ReadStrU64(){return ReadStrInt("u64");}

    char ReadChar()
    {
        SkipSpaces();
        ParseSpecificChar('\'');
        var c = GetChar();
        ParseSpecificChar('\'');
        return c.Value;
    }

    float ReadStrHexFloat(char sign)
    {
        var int_part = ParseHexInt();
        ParseSpecificChar('.');
        var frac_part = ParseHexInt();
        ParseSpecificChar('p');
        var exponent = ParseHexInt();

        var int_val = Convert.ToInt32(int_part, 16);
        var frac_val = Convert.ToSingle(Convert.ToInt32(frac_part, 16)) / Math.Pow(16, frac_part.Length);
        var exp_val = Convert.ToInt32(exponent);

        var total_val = (int_val + frac_val) * Math.Pow(2, exp_val);
        if (sign == '-')
        {
            total_val = -1 * total_val;
        }

        return Convert.ToSingle(total_val);
    }

    float ReadStrDecimal()
    {
        SkipSpaces();
        var c = GetChar();
        char sign;
        if (c.Value == '-')
        {
            sign = '-';
        }
        else
        {
            UngetChar(c.Value);
            sign = '+';
        }

        // Check for hexadecimal float
        c = GetChar();
        if (c.Value == '0' && "xX".Contains(PeekChar()))
        {
            GetChar();
            return ReadStrHexFloat(sign);
        }
        else
        {
            UngetChar(c.Value);
        }

        var bef = Optional(this.ParseInt);
        var aft = "";
        if (string.IsNullOrEmpty(bef))
        {
            bef = "0";
            ParseSpecificChar('.');
            aft = ParseInt();
        }else if (Optional(ParseSpecificChar, '.'))
        {
            aft = ParseInt();
        }
        else
        {
            aft = "0";
        }

        var expt = "";
        if (Optional(ParseSpecificChar, 'E') ||
            Optional(ParseSpecificChar, 'e'))
        {
            expt = ParseIntSigned();
        }
        else
        {
            expt = "0";
        }

        return Convert.ToSingle(sign + bef + "." + aft + "E" + expt);
    }

    float ReadStrF32()
    {
        var x = ReadStrDecimal();
        OptionalSpecificString("f32");
        return x;
    }

    float ReadStrF64()
    {
        var x = ReadStrDecimal();
        OptionalSpecificString("f64");
        return x;
    }

    bool ReadStrBool()
    {
        SkipSpaces();
        if (PeekChar() == 't')
        {
            ParseSpecificString("true");
            return true;
        }

        if (PeekChar() == 'f')
        {
            ParseSpecificString("false");
            return false;
        }

        throw new Exception("ValueError");
    }

    private (T[], int[]) ReadStrArrayElems<T>(int rank, Func<T> ReadStrScalar)
    {
        bool first = true;
        bool[] knows_dimsize = new bool[rank];
        int cur_dim = rank-1;
        int[] elems_read_in_dim = new int[rank];
        int[] shape = new int[rank];

        int capacity = 100;
        T[] data = new T[capacity];
        int write_ptr = 0;

        while (true) {
            SkipSpaces();

            char c = (char) GetChar();
            if (c == ']') {
                if (knows_dimsize[cur_dim]) {
                    if (shape[cur_dim] != elems_read_in_dim[cur_dim]) {
                        throw new Exception("Irregular array");
                    }
                } else {
                    knows_dimsize[cur_dim] = true;
                    shape[cur_dim] = elems_read_in_dim[cur_dim];
                }
                if (cur_dim == 0) {
                    break;
                } else {
                    cur_dim--;
                    elems_read_in_dim[cur_dim]++;
                }
            } else if (c == ',') {
                SkipSpaces();
                c = (char) GetChar();
                if (c == '[') {
                    if (cur_dim == rank - 1) {
                        throw new Exception("Array has too many dimensions");
                    }
                    first = true;
                    cur_dim++;
                    elems_read_in_dim[cur_dim] = 0;
                } else if (cur_dim == rank - 1) {
                    UngetChar(c);

                    data[write_ptr++] = ReadStrScalar();
                    if (write_ptr == capacity) {
                        capacity *= 2;
                        Array.Resize(ref data, capacity);
                    }
                    elems_read_in_dim[cur_dim]++;
                } else {
                    throw new Exception("Unexpected comma when reading array");
                }
            } else if (first) {
                if (c == '[') {
                    if (cur_dim == rank - 1) {
                        throw new Exception("Array has too many dimensions");
                    }
                    cur_dim++;
                    elems_read_in_dim[cur_dim] = 0;
                } else {
                    UngetChar(c);
                    data[write_ptr++] = ReadStrScalar();
                    if (write_ptr == capacity) {
                        capacity *= 2;
                        Array.Resize(ref data, capacity);
                    }
                    elems_read_in_dim[cur_dim]++;
                    first = false;
                }
            } else {
                throw new Exception("Unexpected character in array");
            }
        }
        Array.Resize(ref data, write_ptr);
        return (data, shape);
    }

    public (T[], int[]) ReadStrArrayEmpty<T>(int rank, string typeName, Func<T> ReadStrScalar)
    {
        ParseSpecificString("empty");
        ParseSpecificChar('(');
        for (int i = 0; i < rank; i++) {
            ParseSpecificString("[]");
        }
        ParseSpecificString(typeName);
        ParseSpecificChar(')');

        return (new T[0], new int[rank]);
    }

    public (T[], int[]) ReadStrArray<T>(int rank, string typeName, Func<T> ReadStrScalar)
    {
        long read_dims = 0;

        while (true) {
            SkipSpaces();
            var c = GetChar();
            if (c=='[') {
                read_dims++;
            } else {
                if (c != null) {
                    UngetChar((char)c);
                }
            }
            break;
        }

        if (read_dims == 0) {
            return ReadStrArrayEmpty(rank, typeName, ReadStrScalar);
        }

        if (read_dims != rank) {
            throw new Exception("Wrong number of dimensions");
        }

        return ReadStrArrayElems(rank, ReadStrScalar);
    }

    public sbyte read_i8()
    {
        return (sbyte) ReadStrI8();
    }
    public short read_i16()
    {
        return (short) ReadStrI16();
    }
    public int read_i32()
    {
        return ReadStrI32();
    }
    public long read_i64()
    {
        return ReadStrI64();
    }

    public byte read_u8()
    {
        return (byte) ReadStrU8();
    }
    public ushort read_u16()
    {
        return (ushort) ReadStrU16();
    }
    public uint read_u32()
    {
        return (uint) ReadStrU32();
    }
    public ulong read_u64()
    {
        return (ulong) ReadStrU64();
    }

    public bool read_bool()
    {
        return ReadStrBool();
    }

    public float read_f32()
    {
        return ReadStrDecimal();
    }
    public double read_f64()
    {
        return ReadStrDecimal();
    }
