Imports Luxand
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.Drawing
Imports System
Imports System.Data.SQLite
Imports System.Runtime.InteropServices
Imports System.Threading

#Region "Authentication"
Public Class Form2
    Dim cameraHandle As Integer
    Dim needClose As Boolean
    Dim userName As String
    Const TrackerMemoryFile = "tracker.dat"
    Dim mouseX As Integer = 0
    Dim mouseY As Integer = 0

    Dim currentframe As Image

    Dim sql As String

    Dim cmd As SQLiteCommand
    Dim dr As SQLiteDataReader
    Dim mysqlconn As SQLiteConnection
    Dim FaceListt As List(Of TTFaceRecord)


    'Face Template;
    Dim Template As Byte()
    'Facial Features;
    Dim FacePosition As FSDK.TFacePosition
    Dim FacialFeatures As FSDK.TPoint()

    Dim imagefilename As String
    Dim imagee As FSDK.CImage
    Dim faceImage As FSDK.CImage



    Public Structure TTFaceRecord
        Dim Template As Byte()
        'Face Template;
        Dim FacePosition As FSDK.TFacePosition
        Dim FacialFeatures As FSDK.TPoint()
        'Facial Features;
        Dim ImageFileName As String

        Dim imagee As FSDK.CImage
        Dim faceImage As FSDK.CImage
    End Structure


    Private Sub Form2_FormClosing(sender As Object, e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        needClose = True
        FSDKCam.CloseVideoCamera(cameraHandle)

    End Sub

    Private Sub Form2_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load
        If (FSDK.ActivateLibrary("bSB3NdbTnv/0eW/uhypSe6hDMtjZ76Sisw5NwcN+0sfahxOtoUW22el54e/M6cSG5/xsdVIorPgugbTIfoIIn7ltyw1QMSleNebVx/Xe8aRA8bP+aVDybjoWdW/0rDP9Pv7yqBzNXyuwjgsVhPB53VGP8oTirTSUP7PTzSwOEe0=") <> FSDK.FSDKE_OK) Then
            MessageBox.Show("Please run the License Key Wizard (Start - Luxand - FaceSDK - License Key Wizard)", "Error activating FaceSDK")
            Close()
        End If


        FSDK.InitializeLibrary()
        FSDKCam.InitializeCapturing()


        Dim cameralist() As String = Nothing
        Dim count As Integer
        'Gets camera list
        FSDKCam.GetCameraList(cameralist, count)

        'verifies if there is a camera. I'm always using the first camera detected cameralist(0)
        If (0 = count) Then
            MessageBox.Show("Please attach a camera", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Close()
        End If

        Dim formatList() As FSDKCam.VideoFormatInfo
        FSDKCam.GetVideoFormatList(cameralist(0), formatList, count)
      
        Dim cameraName As String
        cameraName = cameralist(0)
        If (FSDKCam.OpenVideoCamera(cameraName, cameraHandle) <> FSDK.FSDKE_OK) Then
            MessageBox.Show("Error opening the first camera", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Close()
        End If

        FaceListt = New List(Of TTFaceRecord)
        loadme()
    End Sub

    Dim programState As ProgramStates
    ' WinAPI procedure to release HBITMAP handles returned by FSDKCam.GrabFrame
    Declare Auto Function DeleteObject Lib "gdi32.dll" (ByVal hObject As IntPtr) As Boolean
    Enum ProgramStates
        psRemember
        psRecognize
    End Enum


    Public Sub Go(SearchFace As TTFaceRecord)
        Dim img As Image = SearchFace.imagee.ToCLRImage
       
        listView1.Items.Clear()
        ImageList1.Images.Clear()
        '--------------------------------------------------------------------------
        listView1.OwnerDraw = False
        listView1.View = View.LargeIcon
        'listView1.Dock = DockStyle.Bottom
        listView1.LargeImageList = ImageList1

        'GroupBox1.Dock = DockStyle.Bottom

        Dim Threshold As Single = 0.0F
        FSDK.GetMatchingThresholdAtFAR(100 / 100, Threshold)

        Dim MatchedCount As Integer = 0
        Dim FaceCount As Integer = FaceListt.Count
        Dim Similarities As Single() = New Single(FaceCount - 1) {}
        Dim Numbers As Integer() = New Integer(FaceCount - 1) {}

        For i As Integer = 0 To FaceListt.Count - 1
            If i = FaceListt.Count Then
                Exit For
            Else

                Dim Similarity As Single = 0.0F
                Dim CurrentFace As TTFaceRecord = FaceListt(i)
                FSDK.MatchFaces(SearchFace.Template, CurrentFace.Template, Similarity)
                If Similarity >= Threshold Then
                    Similarities(MatchedCount) = Similarity
                    Numbers(MatchedCount) = i
                    MatchedCount += 1
                End If
            End If

        Next

        If MatchedCount = 0 Then
            MessageBox.Show("No matches found. You can try to increase the FAR parameter in the Options dialog box.", "No matches")
        Else
            Dim cmp As New floatReverseComparer()
            Array.Sort(Similarities, Numbers, 0, MatchedCount, DirectCast(cmp, IComparer(Of Single)))
            GroupBox1.Text = "Faces Matched: " + MatchedCount.ToString()

            Try

                'MatchedCount = MatchedCount - 1

                For i As Integer = 0 To MatchedCount - 1

                    ImageList1.Images.Add(FaceListt(Numbers(i)).faceImage.ToCLRImage())

                    listView1.Items.Add((Similarities(i) * 100.0F).ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat), FaceListt(Numbers(i)).ImageFileName & vbCr & vbLf & "Similarity = " & (Similarities(i) * 100).ToString(), ImageList1.Images.Count - 1)
                    'listView1.Items.Add((ImageList1.Images.Count - 1).ToString(), TextBox1.Text, ImageList1.Images.Count - 1)

                Next
            Catch ex As Exception
                MsgBox(ex.ToString)
            End Try

        End If


        Me.Show()

        Try
            If (Similarities(0) * 100.0F) > 50 Then

                PictureBox4.Image = ImageList1.Images(0)
                Label1.Text = FaceListt(Numbers(0)).ImageFileName

            ElseIf (Similarities(0) * 100.0F) < 50 Then
                PictureBox4.Image = My.Resources.no_match_found
                Label1.Text = "No match!"
            End If
        Catch ex As SQLiteException
            MsgBox(ex.ToString)
        End Try


    End Sub



    Private Sub InitializeCameraToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles InitializeCameraToolStripMenuItem.Click
        detect2()
    End Sub

    Sub detect2()
        Dim tracker As Integer = 0  ' creating a Tracker
        If (FSDK.FSDKE_OK <> FSDK.LoadTrackerMemoryFromFile(tracker, TrackerMemoryFile)) Then ' try to load saved tracker state
            FSDK.CreateTracker(tracker) ' if could not be loaded, create a new tracker
        End If

        Dim err As Integer = 0 ' set realtime face detection parameters
        FSDK.SetTrackerMultipleParameters(tracker, "HandleArbitraryRotations=false; DetermineFaceRotationAngle=false; InternalResizeWidth=100; FaceDetectionThreshold=5;", err)


        Dim image As FSDK.CImage
        Dim frameImage As Image
        While Not needClose
            Dim ImageHandle As Integer
            If (FSDKCam.GrabFrame(cameraHandle, ImageHandle) <> FSDK.FSDKE_OK) Then ' grab the current frame from the camera
                Application.DoEvents()
                Continue While
            End If

            image = New FSDK.CImage(ImageHandle)

            Dim IDs() As Long
            ReDim IDs(0 To 256)
            Dim faceCount As Long
            Dim sizeOfLong = 8
            FSDK.FeedFrame(tracker, 0, image.ImageHandle, faceCount, IDs, sizeOfLong * 256) ' maximum 256 faces detected
            Array.Resize(IDs, faceCount)

            frameImage = image.ToCLRImage()
            currentframe = frameImage
            Dim gr As Graphics
            gr = Graphics.FromImage(frameImage)


            Dim i As Integer
            For i = 0 To IDs.Length - 1
                Dim facePosition As FSDK.TFacePosition
                facePosition = New FSDK.TFacePosition
                FSDK.GetTrackerFacePosition(tracker, 0, IDs(i), facePosition)



                Dim left As Integer
                Dim top As Integer
                Dim w As Integer
                left = facePosition.xc - CInt(facePosition.w * 0.6)
                top = facePosition.yc - CInt(facePosition.w * 0.5)
                w = facePosition.w * 1.2

                Dim name As String
                Dim res As Integer
                res = FSDK.GetAllNames(tracker, IDs(i), name, 65536) ' maximum of 65536 characters
                If (FSDK.FSDKE_OK = res And name.Length > 0) Then ' draw name
                    Dim format As New StringFormat()
                    format.Alignment = StringAlignment.Center

                    'gr.DrawString(name, New System.Drawing.Font("Arial", 16), _
                    'New System.Drawing.SolidBrush(System.Drawing.Color.LightGreen), _
                    'facePosition.xc, top + w + 5, format)
                End If

                Dim pen As Pen = Pens.LightGreen

                If (mouseX >= left And mouseX <= left + w And mouseY >= top And mouseY <= top + w) Then
                    pen = Pens.Blue
                    
                End If
            Next


            programState = ProgramStates.psRecognize

            PictureBox1.Image = frameImage ' display current frame

            GC.Collect() ' collect the garbage after the deletion
            Application.DoEvents() ' make UI controls accessible
        End While

        FSDK.SaveTrackerMemoryToFile(tracker, TrackerMemoryFile)

        FSDK.FreeTracker(tracker)

        'FSDKCam.CloseVideoCamera(cameraHandle)
        FSDKCam.FinalizeCapturing()
    End Sub

    Private Sub Button1_Click(sender As System.Object, e As System.EventArgs) Handles Button1.Click
        captureface()
    End Sub

    Sub captureface()

        loadme()

        PictureBox2.Image = currentframe



        FSDK.SetFaceDetectionParameters(False, True, 384)
        FSDK.SetFaceDetectionThreshold(CInt(3))

        Dim fr As New TTFaceRecord()
        fr.ImageFileName = TextBox1.Text
        fr.FacePosition = New FSDK.TFacePosition()
        fr.FacialFeatures = New FSDK.TPoint(1) {}
        fr.Template = New Byte(FSDK.TemplateSize - 1) {}
        'image = New FSDK.CImage(currentframe)
        fr.imagee = New FSDK.CImage(PictureBox2.Image)
        fr.FacePosition = fr.imagee.DetectFace()

        fr.faceImage = fr.imagee.CopyRect(CInt(fr.FacePosition.xc - Math.Round(fr.FacePosition.w * 0.5)), CInt(fr.FacePosition.yc - Math.Round(fr.FacePosition.w * 0.5)), CInt(fr.FacePosition.xc + Math.Round(fr.FacePosition.w * 0.5)), CInt(fr.FacePosition.yc + Math.Round(fr.FacePosition.w * 0.5)))

        Try
            fr.FacialFeatures = fr.imagee.DetectEyesInRegion(fr.FacePosition)
        Catch ex2 As Exception
            MessageBox.Show(ex2.Message, "Error detecting eyes.")
        End Try

        Try

            fr.Template = fr.imagee.GetFaceTemplateInRegion(fr.FacePosition)
        Catch ex2 As Exception
            MessageBox.Show(ex2.Message, "Error retrieving face template.")
        End Try

        PictureBox3.Image = (fr.faceImage.ToCLRImage())
        Go(fr)
    End Sub

    Private Sub Button2_Click(sender As System.Object, e As System.EventArgs) Handles Button2.Click
        temp.PictureBox1.Image = FaceListt(0).faceImage.ToCLRImage()
        temp.Show()

    End Sub


    '--------------------- saves face features into database ------------------------------
    Sub loadme()
        FaceListt.Clear()
        Dim checkstate As String = ""
        listView1.Items.Clear()
        mysqlconn = New SQLiteConnection
        mysqlconn.ConnectionString = Form1.connection
        mysqlconn.Open()
        Dim sqlstr As String = "SELECT ImageFileName, FacePositionXc, FacePositionYc, FacePositionW, FacePositionAngle, Eye1X, Eye1Y, Eye2X, Eye2Y, Template, Image, FaceImage FROM Faceinfo"
        Dim cmd = New SQLiteCommand(sqlstr, mysqlconn)
        Dim sqlReader As SQLiteDataReader = cmd.ExecuteReader
        For i As Integer = 1 To sqlReader.FieldCount

            listView1.BeginUpdate()
            While sqlReader.Read

                Dim fr As New TTFaceRecord()

                fr.ImageFileName = sqlReader.GetString(0)

                fr.FacePosition = New FSDK.TFacePosition()
                fr.FacePosition.xc = sqlReader.GetInt32(1)
                fr.FacePosition.yc = sqlReader.GetInt32(2)
                fr.FacePosition.w = sqlReader.GetInt32(3)
                fr.FacePosition.angle = sqlReader.GetFloat(4)

                fr.FacialFeatures = New FSDK.TPoint(1) {}
                fr.FacialFeatures(0) = New FSDK.TPoint()
                fr.FacialFeatures(0).x = sqlReader.GetInt32(5)
                fr.FacialFeatures(0).y = sqlReader.GetInt32(6)
                fr.FacialFeatures(1) = New FSDK.TPoint()
                fr.FacialFeatures(1).x = sqlReader.GetInt32(7)
                fr.FacialFeatures(1).y = sqlReader.GetInt32(8)

                fr.Template = New Byte(FSDK.TemplateSize - 1) {}
                sqlReader.GetBytes(9, 0, fr.Template, 0, FSDK.TemplateSize)

                Dim img As Image = Image.FromStream(New System.IO.MemoryStream(DirectCast(sqlReader.GetValue(10), Byte())))
                Dim img_face As Image = Image.FromStream(New System.IO.MemoryStream(DirectCast(sqlReader.GetValue(11), Byte())))

                fr.imagee = New FSDK.CImage(img)
                fr.faceImage = New FSDK.CImage(img_face)


                FaceListt.Add(fr)

                img.Dispose()
                img_face.Dispose()

            End While
            listView1.EndUpdate()
        Next


        mysqlconn.Close()
        cmd.Dispose()
        sqlReader.Dispose()
    End Sub

    '-----------------------------------------------------------------------------------

    Public Class floatReverseComparer
        Implements System.Collections.Generic.IComparer(Of Single)


        Public Function Compare1(x As Single, y As Single) As Integer Implements System.Collections.Generic.IComparer(Of Single).Compare
            Return y.CompareTo(x)
        End Function
    End Class

    Private Sub Form2_Shown(sender As Object, e As System.EventArgs) Handles Me.Shown

        InitializeCameraToolStripMenuItem.PerformClick()
    End Sub

    Private Sub CloseToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs)
        needClose = True
        Me.Close()
    End Sub

    Private Sub Button3_Click(sender As System.Object, e As System.EventArgs) Handles Button3.Click

        listView1.Items.Clear()
        PictureBox3.Image = Nothing
        PictureBox4.Image = Nothing



        Dim dlg As New OpenFileDialog
        dlg.Filter = "All Files|*.*|Bitmaps|*.bmp|GIFs|*.gif|JPEGs|*.jpg"
        dlg.Multiselect = True

        Try
            If dlg.ShowDialog = Windows.Forms.DialogResult.OK Then

                FSDKCam.CloseVideoCamera(cameraHandle)


                PictureBox1.Image = Image.FromFile(dlg.FileName)
                PictureBox2.Image = Image.FromFile(dlg.FileName)



                FSDK.SetFaceDetectionParameters(False, True, 384)
                FSDK.SetFaceDetectionThreshold(CInt(3))

                Dim fr As New TTFaceRecord()
                fr.ImageFileName = ""
                fr.FacePosition = New FSDK.TFacePosition()
                fr.FacialFeatures = New FSDK.TPoint(1) {}
                fr.Template = New Byte(FSDK.TemplateSize - 1) {}
                'image = New FSDK.CImage(currentframe)
                fr.imagee = New FSDK.CImage(PictureBox2.Image)
                fr.FacePosition = fr.imagee.DetectFace()


                If 0 = fr.FacePosition.w Then
                    If dlg.FileNames.Length <= 1 Then
                        MessageBox.Show("No faces found", "Enrollment error")
                        Exit Sub
                    Else
                        MessageBox.Show("No faces found" & vbCr & vbLf)
                        Exit Sub
                    End If
                Else

                    fr.faceImage = fr.imagee.CopyRect(CInt(fr.FacePosition.xc - Math.Round(fr.FacePosition.w * 0.5)), CInt(fr.FacePosition.yc - Math.Round(fr.FacePosition.w * 0.5)), CInt(fr.FacePosition.xc + Math.Round(fr.FacePosition.w * 0.5)), CInt(fr.FacePosition.yc + Math.Round(fr.FacePosition.w * 0.5)))

                    Try
                        fr.FacialFeatures = fr.imagee.DetectEyesInRegion(fr.FacePosition)
                    Catch ex2 As Exception
                        MessageBox.Show(ex2.Message, "Error detecting eyes.")
                    End Try

                    Try
                        ' get template with higher precision
                        fr.Template = fr.imagee.GetFaceTemplateInRegion(fr.FacePosition)
                    Catch ex2 As Exception
                        MessageBox.Show(ex2.Message, "Error retrieving face template.")
                    End Try


                    PictureBox3.Image = (fr.faceImage.ToCLRImage())

                   
                    Go(fr)

                End If
            End If

        Catch ex As Exception

        End Try


    End Sub
End Class

#End Region
