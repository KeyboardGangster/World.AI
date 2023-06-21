using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class Base36
{
    public static ulong ToBase10(this string base36)
    {
        base36 = base36.TrimStart('0');

        int pow = 0;
        ulong current = 0;

        foreach(char c in base36.Reverse())
        {
            current += GetBase10Digit(c) * (ulong)Mathf.Pow(36, pow++);
        }

        return current;
    }

    public static string ToBase36(this ulong base10)
    {
        ulong quotient = base10;
        ulong remainder;
        StringBuilder sb = new StringBuilder();

        do
        {
            remainder = quotient % 36;
            quotient /= 36;
            sb.Insert(0, GetBase36Digit(remainder));
        }
        while (quotient > 0);

        return sb.ToString();
    }

    private static string GetBase36Digit(ulong base10)
    {
        if (base10 < 10)
            return base10.ToString();

        switch (base10)
        {
            case 10: return "A";
            case 11: return "B";
            case 12: return "C";
            case 13: return "D";
            case 14: return "E";
            case 15: return "F";
            case 16: return "G";
            case 17: return "H";
            case 18: return "I";
            case 19: return "J";
            case 20: return "K";
            case 21: return "L";
            case 22: return "M";
            case 23: return "N";
            case 24: return "O";
            case 25: return "P";
            case 26: return "Q";
            case 27: return "R";
            case 28: return "S";
            case 29: return "T";
            case 30: return "U";
            case 31: return "V";
            case 32: return "W";
            case 33: return "X";
            case 34: return "Y";
            case 35: return "Z";
            default:
                throw new System.ArgumentException();
        }
    }

    private static ulong GetBase10Digit(char base36)
    {
        switch (char.ToUpper(base36))
        {
            case '0': return 0;
            case '1': return 1;
            case '2': return 2;
            case '3': return 3;
            case '4': return 4;
            case '5': return 5;
            case '6': return 6;
            case '7': return 7;
            case '8': return 8;
            case '9': return 9;
            case 'A': return 10;
            case 'B': return 11;
            case 'C': return 12;
            case 'D': return 13;
            case 'E': return 14;
            case 'F': return 15;
            case 'G': return 16;
            case 'H': return 17;
            case 'I': return 18;
            case 'J': return 19;
            case 'K': return 20;
            case 'L': return 21;
            case 'M': return 22;
            case 'N': return 23;
            case 'O': return 24;
            case 'P': return 25;
            case 'Q': return 26;
            case 'R': return 27;
            case 'S': return 28;
            case 'T': return 29;
            case 'U': return 30;
            case 'V': return 31;
            case 'W': return 32;
            case 'X': return 33;
            case 'Y': return 34;
            case 'Z': return 35;
            default:
                throw new System.ArgumentException();
        }
    }
}
