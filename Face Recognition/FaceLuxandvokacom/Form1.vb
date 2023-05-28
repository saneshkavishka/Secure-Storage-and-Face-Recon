Imports Luxand
'Imports MySql.Data.MySqlClient
Imports System.Data.SQLite
Imports System.IO

#Region "Enrollment codes"
Public Class Form1
    Dim cameraHandle As Integer
    Dim needClose As Boolean
    Dim userName As String
    Const TrackerMemoryFile = "tracker.dat"
    Dim mouseX As Integer = 0
    Dim mouseY As Integer = 0
    Public Shared FaceList As List(Of TFaceRecord)
    Dim currentframe As Image

    Dim sql As String


    Dim cmd As SQLiteCommand
    Dim dr As SQLiteDataReader
    Dim mysqlconn As SQLiteConnection

   
    Public Shared connection = "Data Source=  " & Application.StartupPath & "\facefaceluxand.db;Version=3;"

    Dim Template As Byte()

    Dim FacePosition As FSDK.TFacePosition
    Dim FacialFeatures As FSDK.TPoint()
    Dim imagefilename As String
    Dim imagee As FSDK.CImage
    Dim faceImage As FSDK.CImage
    Dim frominputbox As String

    Private Sub Form1_FormClosing(sender As Object, e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        needClose = True
        Splash.Close()
        Application.Exit()
    End Sub

    Private Sub Form1_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load

        If (FSDK.ActivateLibrary("bSB3NdbTnv/0eW/uhypSe6hDMtjZ76Sisw5NwcN+0sfahxOtoUW22el54e/M6cSG5/xsdVIorPgugbTIfoIIn7ltyw1QMSleNebVx/Xe8aRA8bP+aVDybjoWdW/0rDP9Pv7yqBzNXyuwjgsVhPB53VGP8oTirTSUP7PTzSwOEe0=") <> FSDK.FSDKE_OK) Then
            MessageBox.Show("Please run the License Key Wizard (Start - Luxand - FaceSDK - License Key Wizard)", "Error activating FaceSDK")
            Close()
        End If

        listView1.OwnerDraw = False
        listView1.View = View.LargeIcon
        'listView1.Dock = DockStyle.Right
        listView1.LargeImageList = ImageList1
    End Sub


    Sub startcapturung()
        FSDK.InitializeLibrary()
        FSDKCam.InitializeCapturing()

        Dim cameralist() As String = Nothing
        Dim count As Integer
        FSDKCam.GetCameraList(cameralist, count)

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
        FaceList = New List(Of TFaceRecord)
        loadme()

        detect2()
    End Sub


    Enum ProgramStates
        psRemember
        psRecognize
    End Enum

    Dim programState As ProgramStates
    ' WinAPI procedure to release HBITMAP handles returned by FSDKCam.GrabFrame
    Declare Auto Function DeleteObject Lib "gdi32.dll" (ByVal hObject As IntPtr) As Boolean

    Private Sub PictureBox1_MouseLeave(ByVal sender As Object, ByVal e As System.EventArgs) Handles PictureBox1.MouseLeave
        mouseX = 0
        mouseY = 0
    End Sub

    Private Sub PictureBox1_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles PictureBox1.MouseMove
        mouseX = e.X
        mouseY = e.Y
    End Sub

    Private Sub PictureBox1_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles PictureBox1.MouseUp
        programState = ProgramStates.psRemember
    End Sub

    Private Sub InitializeCameraToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles InitializeCameraToolStripMenuItem.Click
        startcapturung()
    End Sub

    Private Sub GetCurrentframeToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles GetCurrentframeToolStripMenuItem.Click
        If listView1.Items.Count = 0 Then
            MessageBox.Show("Please enroll faces first", "Error", MessageBoxButtons.OK)
        ElseIf listView1.Items.Count > 0 Then

            FSDKCam.CloseVideoCamera(0)

            'Form2.Close()
            'Form2.ShowDialog()
            Dim recog As New Form2
            recog.ShowDialog()
        End If
    End Sub


    

    Sub detect2()
        needClose = False

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

                    gr.DrawString("", New System.Drawing.Font("Arial", 16), _
                    New System.Drawing.SolidBrush(System.Drawing.Color.LightGreen), _
                    facePosition.xc, top + w + 5, format)
                End If

                Dim pen As Pen = Pens.LightGreen

                If (mouseX >= left And mouseX <= left + w And mouseY >= top And mouseY <= top + w) Then
                    pen = Pens.Blue
                    ''-----------------------------------------------------------------------------------

                    ''---------------------------------------------------------------------------------



                    If (ProgramStates.psRemember = programState) Then
                        If (FSDK.FSDKE_OK = FSDK.LockID(tracker, IDs(i))) Then

                            userName = InputBox("Your name:", "Enter your name") 'get the user name
                            If userName Is Nothing Or userName.Length <= 0 Then
                                'FSDK.SetName(tracker, IDs(i), "")
                                'FSDK.PurgeID(tracker, IDs(i))

                            Else
                                'FSDK.SetName(tracker, IDs(i), userName)
                                frominputbox = userName
                                savefrominputbox()

                            End If
                            FSDK.UnlockID(tracker, IDs(i))
                        End If
                    End If

                End If
                gr.DrawRectangle(pen, left, top, w, w)
            Next
            programState = ProgramStates.psRecognize

            PictureBox1.Image = frameImage ' display current frame

            GC.Collect() ' collect the garbage after the deletion
            Application.DoEvents() ' make UI controls accessible
        End While

        FSDK.SaveTrackerMemoryToFile(tracker, TrackerMemoryFile)

        FSDK.FreeTracker(tracker)

        FSDKCam.CloseVideoCamera(cameraHandle)
        FSDKCam.FinalizeCapturing()
    End Sub

    Sub captureface()

        Try
            PictureBox2.Image = currentframe



            FSDK.SetFaceDetectionParameters(False, True, 384)
            FSDK.SetFaceDetectionThreshold(CInt(3))



            imagefilename = TextBox1.Text
            FacePosition = New FSDK.TFacePosition()
            FacialFeatures = New FSDK.TPoint(1) {}
            Template = New Byte(FSDK.TemplateSize - 1) {}
            'image = New FSDK.CImage(currentframe)
            imagee = New FSDK.CImage(PictureBox2.Image)
            FacePosition = imagee.DetectFace()

            faceImage = imagee.CopyRect(CInt(FacePosition.xc - Math.Round(FacePosition.w * 0.5)), CInt(FacePosition.yc - Math.Round(FacePosition.w * 0.5)), CInt(FacePosition.xc + Math.Round(FacePosition.w * 0.5)), CInt(FacePosition.yc + Math.Round(FacePosition.w * 0.5)))

            Try
                FacialFeatures = imagee.DetectEyesInRegion(FacePosition)
            Catch ex2 As Exception
                MessageBox.Show(ex2.Message, "Error detecting eyes.")
            End Try

            Try
                ' get template with higher precision
                Template = imagee.GetFaceTemplateInRegion(FacePosition)
            Catch ex2 As Exception
                MessageBox.Show(ex2.Message, "Error retrieving face template.")
            End Try

            'ImageList1.Images.Add(faceImage.ToCLRImage())
            'PictureBox3.Image = (faceImage.ToCLRImage())
            'ImageList1.Images.Add(faceImage.ToCLRImage())
            'listView1.Items.Add((ImageList1.Images.Count - 1).ToString(), imagefilename, ImageList1.Images.Count - 1)

            'listView1.SelectedIndices.Clear()
            'listView1.SelectedIndices.Add(listView1.Items.Count - 1)

        Catch ex As Exception
            MsgBox("Please initialize camera to use this function", MsgBoxStyle.Exclamation)
        End Try





    End Sub

    Private Sub Button2_Click(sender As System.Object, e As System.EventArgs) Handles Button2.Click
        If TextBox1.Text = "" Or String.IsNullOrEmpty(TextBox1.Text) Then
            MsgBox("Please enter name of current person enrolling", MsgBoxStyle.Information)

        Else
            captureface()


            Dim img As Image = Nothing
            Dim img_face As Image = Nothing
            Dim strm As New MemoryStream()
            Dim strm_face As New MemoryStream()
            img = imagee.ToCLRImage()
            img_face = faceImage.ToCLRImage()
            img.Save(strm, System.Drawing.Imaging.ImageFormat.Jpeg)
            img_face.Save(strm_face, System.Drawing.Imaging.ImageFormat.Jpeg)
            Dim img_array As Byte() = New Byte(strm.Length - 1) {}
            Dim img_face_array As Byte() = New Byte(strm_face.Length - 1) {}
            strm.Position = 0
            strm.Read(img_array, 0, img_array.Length)
            strm_face.Position = 0
            strm_face.Read(img_face_array, 0, img_face_array.Length)

            Dim iReturn As Boolean
            mysqlconn = New SQLiteConnection

            mysqlconn.ConnectionString = connection

            'Dim sqlCommand As New MySqlCommand
            Using sqlCommand As New SQLiteCommand()
                With sqlCommand
                    '.CommandText = "INSERT INTO userinfo (name,username,image) values (@name,@username,@image)"
                    .CommandText = "INSERT INTO Faceinfo(ImageFileName, FacePositionXc, FacePositionYc, FacePositionW, FacePositionAngle, Eye1X, Eye1Y, Eye2X, Eye2Y, Template, Image, FaceImage) " _
                 & " values(@ImageFileName, @FacePositionXc, @FacePositionYc, @FacePositionW, @FacePositionAngle, @Eye1X, @Eye1Y, @Eye2X, @Eye2Y, @Template, @Image, @FaceImage)"
                    .Connection = mysqlconn
                    .CommandType = CommandType.Text


                    .Parameters.AddWithValue("@ImageFileName", imagefilename)
                    .Parameters.AddWithValue("@FacePositionXc", FacePosition.xc)
                    .Parameters.AddWithValue("@FacePositionYc", FacePosition.yc)
                    .Parameters.AddWithValue("@FacePositionW", FacePosition.w)
                    .Parameters.AddWithValue("@FacePositionAngle", FacePosition.angle)
                    .Parameters.AddWithValue("@Eye1X", FacialFeatures(0).x)
                    .Parameters.AddWithValue("@Eye1Y", FacialFeatures(0).y)
                    .Parameters.AddWithValue("@Eye2X", FacialFeatures(1).x)
                    .Parameters.AddWithValue("@Eye2Y", FacialFeatures(1).y)
                    .Parameters.AddWithValue("@Template", Template)
                    .Parameters.AddWithValue("@Image", img_array)
                    .Parameters.AddWithValue("@FaceImage", img_face_array)

                End With
                Try
                    mysqlconn.Open()
                    sqlCommand.ExecuteNonQuery()
                    iReturn = True



                    'Dim number As String
                    'number = DataGridView1.Rows.Count.ToString
                    'Dim strBasePath As String

                    'Dim strFileName As String
                    'strFileName = number + ".jpg"
                    'strBasePath = Application.StartupPath & "\Capturedfaces"
                    'If Directory.Exists(strBasePath) = False Then
                    '    Call Directory.CreateDirectory(strBasePath)

                    'End If


                    'Call PictureBox3.Image.Save(strBasePath & "\" & strFileName, System.Drawing.Imaging.ImageFormat.Jpeg)
                    'MsgBox(number + " " + "has been captured successfully", MsgBoxStyle.Information)


                    TextBox1.Text = ""
                    TextBox1.Focus()


                    MsgBox(imagefilename & " " & "has been captured successfully", MsgBoxStyle.Information)

                Catch ex As SQLiteException
                    MsgBox(ex.Message.ToString)
                    iReturn = False
                Finally
                    mysqlconn.Close()
                    img.Dispose()
                    img_face.Dispose()
                    loadme()
                End Try
            End Using




        End If

    End Sub


    Public Structure TFaceRecord
        Dim Template As Byte()
        'Face Template;
        Dim FacePosition As FSDK.TFacePosition
        Dim FacialFeatures As FSDK.TPoint()
        'Facial Features;
        Dim ImageFileName As String

        Dim imagee As FSDK.CImage
        Dim faceImage As FSDK.CImage
    End Structure

    Sub loadme()

        FaceList.Clear()
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

                Dim fr As New TFaceRecord()

                fr.ImageFileName = sqlReader.GetString(0).ToUpper

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


                FaceList.Add(fr)

                ImageList1.Images.Add(fr.faceImage.ToCLRImage())
                Dim fn As String = fr.ImageFileName
                'listView1.Items.Add((ImageList1.Images.Count - 1).ToString(), fn.Split("\"c)(fn.Split("\"c).Length - 1), ImageList1.Images.Count - 1)
                listView1.Items.Add((ImageList1.Images.Count - 1).ToString(), fn, ImageList1.Images.Count - 1)
                'TextBox1.Text += (Convert.ToString("File '") & fn) + "' read from database" & vbCr & vbLf

                img.Dispose()
                img_face.Dispose()



            End While
            listView1.EndUpdate()
        Next

        mysqlconn.Close()
        cmd.Dispose()
        sqlReader.Dispose()
    End Sub


    Sub loadme2()

        Dim checkstate As String = ""
        Dim arrImage() As Byte = Nothing
        Dim arrImage2() As Byte = Nothing

        listView1.Items.Clear()
        mysqlconn = New SQLiteConnection
        mysqlconn.ConnectionString = connection
        mysqlconn.Open()
        Dim sqlstr As String = "SELECT ImageFileName, FacePositionXc, FacePositionYc, FacePositionW, FacePositionAngle, Eye1X, Eye1Y, Eye2X, Eye2Y, Template, Image, FaceImage FROM Faceinfo"
        Dim cmd = New SQLiteCommand(sqlstr, mysqlconn)
        Dim sqlReader As SQLiteDataReader = cmd.ExecuteReader
        For i As Integer = 1 To sqlReader.FieldCount

            listView1.BeginUpdate()
            While sqlReader.Read

                Dim fr As New TFaceRecord()
                imagefilename = sqlReader("ImageFileName")

                FacePosition = New FSDK.TFacePosition()
                FacePosition.xc = sqlReader("FacePositionXc")
                FacePosition.yc = sqlReader("FacePositionYc")
                FacePosition.w = sqlReader("FacePositionW")
                FacePosition.angle = sqlReader("FacePositionAngle")

                FacialFeatures = New FSDK.TPoint(1) {}
                FacialFeatures(0) = New FSDK.TPoint()
                FacialFeatures(0).x = sqlReader("Eye1X")
                FacialFeatures(0).y = sqlReader("Eye1Y")
                FacialFeatures(1) = New FSDK.TPoint()
                FacialFeatures(1).x = sqlReader("Eye2X")
                FacialFeatures(1).y = sqlReader("Eye2Y")

                Template = New Byte(FSDK.TemplateSize - 1) {}
                sqlReader.GetBytes(9, 0, Template, 0, FSDK.TemplateSize)

                arrImage = sqlReader("Image")
                arrImage2 = sqlReader("FaceImage")

                Dim mstream As New System.IO.MemoryStream(arrImage)
                Dim mstreamm As New System.IO.MemoryStream(arrImage2)

                Dim img As Image = Image.FromStream(mstream)
                Dim img_face As Image = Image.FromStream(mstreamm)

                imagee = New FSDK.CImage(img)
                faceImage = New FSDK.CImage(img_face)


                'FaceList.Add(fr)

                ImageList1.Images.Add(faceImage.ToCLRImage())
                Dim fn As String = imagefilename
                'listView1.Items.Add((ImageList1.Images.Count - 1).ToString(), fn.Split("\"c)(fn.Split("\"c).Length - 1), ImageList1.Images.Count - 1)
                listView1.Items.Add((ImageList1.Images.Count - 1).ToString(), fn, ImageList1.Images.Count - 1)
                'TextBox1.Text += (Convert.ToString("File '") & fn) + "' read from database" & vbCr & vbLf

                img.Dispose()
                img_face.Dispose()



            End While
            listView1.EndUpdate()
        Next

        mysqlconn.Close()
        cmd.Dispose()
        sqlReader.Dispose()
    End Sub

   
    Private Sub Button3_Click(sender As System.Object, e As System.EventArgs)
        loadme()
    End Sub

    Private Sub Button4_Click(sender As System.Object, e As System.EventArgs)
        temp.PictureBox1.Image = FaceList(0).faceImage.ToCLRImage()
        temp.Show()

    End Sub

    Private Sub ViewStoredDataToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles ViewStoredDataToolStripMenuItem.Click
        temp.Close()
        temp.ShowDialog()
    End Sub

    Private Sub Button6_Click(sender As System.Object, e As System.EventArgs) Handles Button6.Click
        If (Button6.Text = "<") Then
            Button6.Text = ">"
            'Button6.Image = My.Resources.arrow_point_to_right
            Panel1.Visible = True
            PictureBox1.Width = 750
            GroupBox2.Location = New Point(151, 517)
        ElseIf (Button6.Text = ">") Then
            Button6.Text = "<"
            'Button6.Image = (My.Resources.scroll_arrow_to_left)
            Panel1.Visible = False
            PictureBox1.Width = 950
            GroupBox2.Location = New Point(266, 517)
        End If
    End Sub

    Sub partofinitialize()
       
    End Sub

    Sub savefrominputbox()
        captureface()


        Dim img As Image = Nothing
        Dim img_face As Image = Nothing
        Dim strm As New MemoryStream()
        Dim strm_face As New MemoryStream()
        img = imagee.ToCLRImage()
        img_face = faceImage.ToCLRImage()
        img.Save(strm, System.Drawing.Imaging.ImageFormat.Jpeg)
        img_face.Save(strm_face, System.Drawing.Imaging.ImageFormat.Jpeg)
        Dim img_array As Byte() = New Byte(strm.Length - 1) {}
        Dim img_face_array As Byte() = New Byte(strm_face.Length - 1) {}
        strm.Position = 0
        strm.Read(img_array, 0, img_array.Length)
        strm_face.Position = 0
        strm_face.Read(img_face_array, 0, img_face_array.Length)

        Dim iReturn As Boolean
        mysqlconn = New SQLiteConnection

        mysqlconn.ConnectionString = connection

        'Dim sqlCommand As New MySqlCommand
        Using sqlCommand As New SQLiteCommand()
            With sqlCommand
                '.CommandText = "INSERT INTO userinfo (name,username,image) values (@name,@username,@image)"
                .CommandText = "INSERT INTO Faceinfo(ImageFileName, FacePositionXc, FacePositionYc, FacePositionW, FacePositionAngle, Eye1X, Eye1Y, Eye2X, Eye2Y, Template, Image, FaceImage) " _
             & " values(@ImageFileName, @FacePositionXc, @FacePositionYc, @FacePositionW, @FacePositionAngle, @Eye1X, @Eye1Y, @Eye2X, @Eye2Y, @Template, @Image, @FaceImage)"
                .Connection = mysqlconn
                .CommandType = CommandType.Text


                .Parameters.AddWithValue("@ImageFileName", frominputbox)
                .Parameters.AddWithValue("@FacePositionXc", FacePosition.xc)
                .Parameters.AddWithValue("@FacePositionYc", FacePosition.yc)
                .Parameters.AddWithValue("@FacePositionW", FacePosition.w)
                .Parameters.AddWithValue("@FacePositionAngle", FacePosition.angle)
                .Parameters.AddWithValue("@Eye1X", FacialFeatures(0).x)
                .Parameters.AddWithValue("@Eye1Y", FacialFeatures(0).y)
                .Parameters.AddWithValue("@Eye2X", FacialFeatures(1).x)
                .Parameters.AddWithValue("@Eye2Y", FacialFeatures(1).y)
                .Parameters.AddWithValue("@Template", Template)
                .Parameters.AddWithValue("@Image", img_array)
                .Parameters.AddWithValue("@FaceImage", img_face_array)

            End With
            Try
                mysqlconn.Open()
                sqlCommand.ExecuteNonQuery()
                iReturn = True



                TextBox1.Text = ""
                TextBox1.Focus()


                MsgBox(frominputbox & " " & "has been captured successfully", MsgBoxStyle.Information)

            Catch ex As SQLiteException
                MsgBox(ex.Message.ToString)
                iReturn = False
            Finally
                mysqlconn.Close()
                img.Dispose()
                img_face.Dispose()
                loadme()
            End Try
        End Using

    End Sub

    Private Sub UploadPictureToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles UploadPictureToolStripMenuItem.Click



        Dim dlg As New OpenFileDialog
        dlg.Filter = "All Files|*.*|Bitmaps|*.bmp|GIFs|*.gif|JPEGs|*.jpg"
        dlg.Multiselect = True

        If dlg.ShowDialog = Windows.Forms.DialogResult.OK Then

            FSDK.SetFaceDetectionParameters(False, True, 384)
            FSDK.SetFaceDetectionThreshold(CInt(3))

            For Each fn As String In dlg.FileNames

                PictureBox1.Image = Image.FromFile(dlg.FileName)
                PictureBox2.Image = Image.FromFile(dlg.FileName)

                Dim nm As String = InputBox("Please enter name of uploaded Picture", "Enter name", "", , )

                If nm = "" Or String.IsNullOrEmpty(nm) Then
                    nm = "Unknown"
                End If

                imagefilename = nm

                FacePosition = New FSDK.TFacePosition()
                FacialFeatures = New FSDK.TPoint(1) {}
                Template = New Byte(FSDK.TemplateSize - 1) {}
                imagee = New FSDK.CImage(PictureBox2.Image)

                FacePosition = imagee.DetectFace()



                If 0 = FacePosition.w Then
                    If dlg.FileNames.Length <= 1 Then
                        MessageBox.Show("No faces found", "Enrollment error")
                        Exit Sub
                    Else
                        MessageBox.Show("No faces found" & vbCr & vbLf)
                        Exit Sub
                    End If
                Else

                    faceImage = imagee.CopyRect(CInt(FacePosition.xc - Math.Round(FacePosition.w * 0.5)), CInt(FacePosition.yc - Math.Round(FacePosition.w * 0.5)), CInt(FacePosition.xc + Math.Round(FacePosition.w * 0.5)), CInt(FacePosition.yc + Math.Round(FacePosition.w * 0.5)))

                    Try
                        FacialFeatures = imagee.DetectEyesInRegion(FacePosition)
                    Catch ex2 As Exception
                        MessageBox.Show(ex2.Message, "Error detecting eyes.")
                    End Try

                    Try
                        ' get template with higher precision
                        Template = imagee.GetFaceTemplateInRegion(FacePosition)
                    Catch ex2 As Exception
                        MessageBox.Show(ex2.Message, "Error retrieving face template.")
                    End Try

                End If




                capturefromupload()



            Next


            loadme()
        End If
    End Sub


    Sub capturefromupload()
        Dim img As Image = Nothing
        Dim img_face As Image = Nothing
        Dim strm As New MemoryStream()
        Dim strm_face As New MemoryStream()
        img = imagee.ToCLRImage()
        img_face = faceImage.ToCLRImage()
        img.Save(strm, System.Drawing.Imaging.ImageFormat.Jpeg)
        img_face.Save(strm_face, System.Drawing.Imaging.ImageFormat.Jpeg)
        Dim img_array As Byte() = New Byte(strm.Length - 1) {}
        Dim img_face_array As Byte() = New Byte(strm_face.Length - 1) {}
        strm.Position = 0
        strm.Read(img_array, 0, img_array.Length)
        strm_face.Position = 0
        strm_face.Read(img_face_array, 0, img_face_array.Length)

        Dim iReturn As Boolean
        mysqlconn = New SQLiteConnection

        mysqlconn.ConnectionString = connection

        'Dim sqlCommand As New MySqlCommand
        Using sqlCommand As New SQLiteCommand()
            With sqlCommand
                '.CommandText = "INSERT INTO userinfo (name,username,image) values (@name,@username,@image)"
                .CommandText = "INSERT INTO Faceinfo(ImageFileName, FacePositionXc, FacePositionYc, FacePositionW, FacePositionAngle, Eye1X, Eye1Y, Eye2X, Eye2Y, Template, Image, FaceImage) " _
             & " values(@ImageFileName, @FacePositionXc, @FacePositionYc, @FacePositionW, @FacePositionAngle, @Eye1X, @Eye1Y, @Eye2X, @Eye2Y, @Template, @Image, @FaceImage)"
                .Connection = mysqlconn
                .CommandType = CommandType.Text


                .Parameters.AddWithValue("@ImageFileName", imagefilename)
                .Parameters.AddWithValue("@FacePositionXc", FacePosition.xc)
                .Parameters.AddWithValue("@FacePositionYc", FacePosition.yc)
                .Parameters.AddWithValue("@FacePositionW", FacePosition.w)
                .Parameters.AddWithValue("@FacePositionAngle", FacePosition.angle)
                .Parameters.AddWithValue("@Eye1X", FacialFeatures(0).x)
                .Parameters.AddWithValue("@Eye1Y", FacialFeatures(0).y)
                .Parameters.AddWithValue("@Eye2X", FacialFeatures(1).x)
                .Parameters.AddWithValue("@Eye2Y", FacialFeatures(1).y)
                .Parameters.AddWithValue("@Template", Template)
                .Parameters.AddWithValue("@Image", img_array)
                .Parameters.AddWithValue("@FaceImage", img_face_array)

            End With
            Try
                mysqlconn.Open()
                sqlCommand.ExecuteNonQuery()
                iReturn = True

                TextBox1.Text = ""
                TextBox1.Focus()


                MsgBox(imagefilename & " " & "has been captured successfully", MsgBoxStyle.Information)

            Catch ex As SQLiteException
                MsgBox(ex.Message.ToString)
                iReturn = False
            Finally
                mysqlconn.Close()
                img.Dispose()
                img_face.Dispose()

            End Try
        End Using
    End Sub

    Private Sub PictureBox1_Click(sender As Object, e As EventArgs) Handles PictureBox1.Click

    End Sub
End Class

#End Region
