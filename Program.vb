Option Strict On
Option Infer On
Imports VbPixelGameEngine

Public NotInheritable Class Program
    Inherits PixelGameEngine

    Private ReadOnly CircleColor As New Pixel(59, 130, 246)
    Private ReadOnly GridColor As New Pixel(229, 231, 235)   
    Private ReadOnly TextColor As New Pixel(31, 41, 55)      
    Private ReadOnly AccentColor As New Pixel(16, 185, 129)   
    
    Private Enum GameState
        StartScreen
        Drawing
        ShowingResult
    End Enum
    
    Private currentState As GameState = GameState.StartScreen
    Private ReadOnly drawingPoints As New List(Of Vi2d)
    Private isDrawing As Boolean = False
    Private score As Integer = 0
    Private roundness As Double = 0
    Private symmetry As Double = 0
    
    Private Const GRID_SIZE As Integer = 20
    
    Public Sub New()
        AppName = "Circle Drawing Game"
    End Sub

    Protected Overrides Function OnUserUpdate(elapsedTime As Single) As Boolean
        Clear(Presets.White)

        Select Case currentState
            Case GameState.StartScreen
                DrawStartScreen()
                If GetKey(Key.ENTER).Pressed Then currentState = GameState.Drawing
            Case GameState.Drawing
                DrawPlayingScreen(True)
                HandleDrawingInput()

            Case GameState.ShowingResult
                DrawPlayingScreen(False)

                If GetKey(Key.ENTER).Pressed Then
                    ResetGame()
                    currentState = GameState.Drawing
                End If
        End Select

        Return Not GetKey(Key.ESCAPE).Pressed
    End Function

    Private Sub DrawStartScreen()
        DrawString(New Vi2d(ScreenWidth \ 2 - 225, ScreenHeight \ 3), "CIRCLE DRAWING GAME", TextColor, 3)
        DrawString(New Vi2d(100, ScreenHeight \ 2 - 60), "Draw a perfect circle with your mouse.", TextColor, 2)
        DrawString(New Vi2d(100, ScreenHeight \ 2 - 30), "Hold left mouse button to draw.", TextColor, 2)
        DrawString(New Vi2d(100, ScreenHeight \ 2), "Score based on roundness & symmetry.", TextColor, 2)
        DrawString(New Vi2d(ScreenWidth \ 2 - 200, ScreenHeight \ 2 + 80), "-> PRESS ""ENTER"" TO START", AccentColor, 2)
    End Sub

    Private Sub DrawPlayingScreen(isPlaying As Boolean)
        DrawGrid()
        If drawingPoints.Count > 1 Then
            For i As Integer = 1 To drawingPoints.Count - 1
                DrawLine(drawingPoints(i - 1), drawingPoints(i), CircleColor)
            Next i
        End If

        If isPlaying Then
            DrawString(New Vi2d(10, 10), If(isDrawing, "Drag to draw the circle. Release when done.",
                       "Hold left mouse button to start drawing."), TextColor, 2)
            Exit Sub
        End If

        Dim ReprScore = If(score = 100, Function(x As Integer) x.ToString(), AddressOf Str)
        DrawString(New Vi2d(ScreenWidth \ 2 - 70, ScreenHeight \ 3 - 10), "YOUR SCORE", TextColor, 2)
        DrawString(New Vi2d(ScreenWidth \ 2 - 50, ScreenHeight \ 2 - 75), ReprScore(score), CircleColor, 5)
        DrawString(New Vi2d(ScreenWidth \ 2 - 125, ScreenHeight \ 2 - 10), $"ROUNDNESS: {CInt(roundness * 100),3}%", TextColor, 2)
        DrawString(New Vi2d(ScreenWidth \ 2 - 125, ScreenHeight \ 2 + 30), $"SYMMETRY:  {CInt(symmetry * 100),3}%", TextColor, 2)
        DrawString(New Vi2d(ScreenWidth \ 2 - 175, ScreenHeight \ 2 + 100), "PRESS ""ENTER"" TO RESTART", AccentColor, 2)
    End Sub

    Private Sub HandleDrawingInput()
        If GetMouse(0).Pressed Then
            isDrawing = True
            drawingPoints.Clear()
            drawingPoints.Add(New Vi2d(GetMouseX, GetMouseY))
        End If

        If GetMouse(0).Held AndAlso isDrawing Then
            drawingPoints.Add(New Vi2d(GetMouseX, GetMouseY))
        End If

        If GetMouse(0).Released AndAlso isDrawing Then
            isDrawing = False
            roundness = CalculateRoundness()
            symmetry = CalculateSymmetry()
            score = CInt((roundness + symmetry) / 2 * 100)
            currentState = GameState.ShowingResult
        End If
    End Sub

    Private Sub DrawGrid()
        For y As Integer = 0 To ScreenHeight Step GRID_SIZE
            For x As Integer = 0 To ScreenWidth
                Draw(New Vi2d(x, y), GridColor)
            Next x
        Next y

        For x As Integer = 0 To ScreenWidth Step GRID_SIZE
            For y As Integer = 0 To ScreenHeight
                Draw(New Vi2d(x, y), GridColor)
            Next y
        Next x
    End Sub

    Private Function CalculateRoundness() As Double
        If drawingPoints.Count < 3 Then Return 0

        Dim center = Aggregate dp In drawingPoints Into x = Average(dp.x), y = Average(dp.y)
        Dim radii As New List(Of Double)
        For Each p As Vi2d In drawingPoints
            Dim dx As Double = p.x - center.x
            Dim dy As Double = p.y - center.y
            radii.Add(Math.Sqrt(dx * dx + dy * dy))
        Next p
        Dim avgRadius = radii.Average()
        Dim variance = Aggregate r In radii Let diff = r - avgRadius Into Average(diff ^ 2)
        Dim stdDev As Double = Math.Sqrt(variance)

        Dim result = 1 - (stdDev / (avgRadius * 0.2))
        Return If(Double.IsNaN(result), 0, Math.Max(0, result))
    End Function

    Private Function CalculateSymmetry() As Double
        If drawingPoints.Count < 3 Then Return 0

        Dim center = Aggregate dp In drawingPoints Into x = Average(dp.x), y = Average(dp.y)

        Dim pointData As New List(Of (Double, Double))
        For Each p As Vi2d In drawingPoints
            Dim dx As Double = p.x - center.x
            Dim dy As Double = p.y - center.y
            pointData.Add((Math.Atan2(dy, dx), Math.Sqrt(dx * dx + dy * dy)))
        Next p
        pointData.Sort(Function(a, b) a.Item1.CompareTo(b.Item1))
        Dim symmetryScore As Double = 0
        Dim numPoints As Integer = pointData.Count

        For i As Integer = 0 To numPoints - 1
            Dim oppositeIndex As Integer = CInt((i + numPoints / 2) Mod numPoints)
            Dim distanceDiff As Double = Math.Abs(pointData(i).Item2 - pointData(oppositeIndex).Item2)
            Dim maxDistance As Double = Math.Max(pointData(i).Item2, pointData(oppositeIndex).Item2)

            If maxDistance > 0 Then symmetryScore += 1 - (distanceDiff / maxDistance)
        Next i

        Dim result = symmetryScore / numPoints
        Return If(Double.IsNaN(result), 0, Math.Max(0, result))
    End Function

    Private Sub ResetGame()
        drawingPoints.Clear()
        score = 0
        roundness = 0
        symmetry = 0
    End Sub

    Friend Shared Sub Main()
        With New Program
            If .Construct(800, 600, fullscreen:=False) Then .Start()
        End With
    End Sub
End Class