using UnityEngine;

// Draws text on the screen so it is easy to read on any background and any screen size:
// the size grows with the screen, the letters get a black outline,
// and there can be a dark see-through panel behind them.
// Every on-screen text in the game (health, messages, the spell list, the title screen) uses it.
public static class ScreenText
{
    // A font size that is a share of the screen height (0.04 = 4% of the screen height).
    public static int SizeFor(float heightShare)
    {
        return Mathf.Max(16, Mathf.RoundToInt(Screen.height * heightShare));
    }

    // Draws the text inside the area. Returns the size the text took up.
    public static Vector2 Draw(Rect textArea, string shownText, int fontSize, TextAnchor textAnchor, Color textColor, bool withPanel)
    {
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = fontSize;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.alignment = textAnchor;
        textStyle.wordWrap = false;
        textStyle.richText = false;
        Vector2 textSize = textStyle.CalcSize(new GUIContent(shownText));

        if (withPanel)
        {
            float panelPadding = fontSize * 0.35f;
            Rect panelRect = PlaceInArea(textArea, textSize, textAnchor);
            panelRect.x -= panelPadding;
            panelRect.y -= panelPadding * 0.5f;
            panelRect.width += panelPadding * 2f;
            panelRect.height += panelPadding;
            Color oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f * textColor.a);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        // the black outline: the text drawn a few pixels off in every direction
        float outlineWidth = Mathf.Max(1f, fontSize / 14f);
        textStyle.normal.textColor = new Color(0f, 0f, 0f, textColor.a);
        for (int stepX = -1; stepX <= 1; stepX++)
        {
            for (int stepY = -1; stepY <= 1; stepY++)
            {
                if (stepX == 0 && stepY == 0)
                {
                    continue;
                }
                Rect outlineRect = new Rect(textArea.x + stepX * outlineWidth, textArea.y + stepY * outlineWidth, textArea.width, textArea.height);
                GUI.Label(outlineRect, shownText, textStyle);
            }
        }
        textStyle.normal.textColor = textColor;
        GUI.Label(textArea, shownText, textStyle);
        return textSize;
    }

    // Where text of this size sits inside the area for the given anchor.
    static Rect PlaceInArea(Rect textArea, Vector2 textSize, TextAnchor textAnchor)
    {
        float placeX = textArea.x;
        float placeY = textArea.y;
        int anchorNumber = (int)textAnchor;
        int columnNumber = anchorNumber % 3;   // 0 left, 1 center, 2 right
        int rowNumber = anchorNumber / 3;      // 0 top, 1 middle, 2 bottom
        if (columnNumber == 1)
        {
            placeX = textArea.x + (textArea.width - textSize.x) / 2f;
        }
        else if (columnNumber == 2)
        {
            placeX = textArea.xMax - textSize.x;
        }
        if (rowNumber == 1)
        {
            placeY = textArea.y + (textArea.height - textSize.y) / 2f;
        }
        else if (rowNumber == 2)
        {
            placeY = textArea.yMax - textSize.y;
        }
        return new Rect(placeX, placeY, textSize.x, textSize.y);
    }
}
