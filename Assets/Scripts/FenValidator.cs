using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class FenValidator
{
    public static bool IsFenStringValid(string fen)
    {
        // Split the FEN into its components
        string[] fenComponents = fen.Split(' ');

        if (fenComponents.Length != 4) { return false; }

        // check of the board position is valid
        if (!IsBoardPositionValid(fenComponents[0])) { return false; }

        // Check if the active color is valid
        if (!IsColorValid(fenComponents[1])) { return false; }

        // Check if castling availability is valid
        if (!IsCastlingAvailabilityValid(fenComponents[2])) { return false; }

        // check if en passant target square is valid
        if (!IsEnPassantTargetValid(fenComponents[3])) { return false; }

        return true;

    }

    private static bool IsBoardPositionValid(string boardPosition)
    {
        //check if the borad position containse exactly 8 rows separated by '/'
        string[] rows = boardPosition.Split('/');
        if (rows.Length != 8) { return false; }

        // check if each row coantains valid pieces (1-8 characters:  p, n, b, r, q, k, P, N, B, R, Q, K)
        foreach (string row in rows)
        {
            if (row.Length < 1 || row.Length > 8) { return false; }

            foreach (char piece in row)
            {
                if (!"12345678pnbrqkPNBRQK".Contains(piece)) { return false; }
            }
        }

        return true;
    }

    private static bool IsColorValid(string color)
    {
        return color == "w" || color == "b";
    }

    private static bool IsCastlingAvailabilityValid(string castlingAvailability)
    {
        return Regex.IsMatch(castlingAvailability, "^(K?Q?k?q?|-)$");
    }

    private static bool IsEnPassantTargetValid(string enPassantTarget)
    {
        return enPassantTarget == "-" || Regex.IsMatch(enPassantTarget, "^[a-h][3-6]$");
    }
}