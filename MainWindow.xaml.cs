using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TSM = Tekla.Structures.Model;
using TSG = Tekla.Structures.Geometry3d;
using TSMUI = Tekla.Structures.Model.UI;
using TSMOP = Tekla.Structures.Model.Operations;
using System.Collections;
using Tekla.Structures.Model.UI;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;
using System.ComponentModel;

namespace Create_Lofted_Plate
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        TSM.Model model;
        public MainWindow()
        {
            InitializeComponent();
        }

        

        private void ButtonCreateDetails_Click(object sender, RoutedEventArgs e)
        {
            model = model ?? new TSM.Model();

            if (!model.GetConnectionStatus())
            {
                MessageBox.Show("Не удалось подключиться к Tekla Structures");
            }

            else
            {
                try
                {
                    //Получаем исходные данные
                    double heightAssembly = Convert.ToDouble(HeightAssembly.Text);
                    double widthBeam = Convert.ToDouble(WidthBeam.Text);
                    double topThickness = Convert.ToDouble(TopThickness.Text);
                    double offsetBeam = Convert.ToDouble(OffsetBeam.Text);
                    double heightWall = Convert.ToDouble(HeightWall.Text);
                    double thicknessWall = Convert.ToDouble(ThicknessWall.Text);
                    double offsetWall = Convert.ToDouble(OffsetWall.Text);

                    string classTop = Convert.ToString(ClassTop.Text);
                    string classBottom = Convert.ToString(ClassBottom.Text);
                    string classWall = Convert.ToString(ClassWall.Text);
                    string material = Convert.ToString(Material.Text);
                    string prefixDetail = Convert.ToString(PrefixDetail.Text);
                    string prefixAssembly = Convert.ToString(PrefixAssembly.Text);

                    //Перечислитель активных видов
                    ModelViewEnumerator viewEnum = ViewHandler.GetVisibleViews();

                    //Получаем исходные точки дуги
                    Picker picker = new TSMUI.Picker();

                    TSG.Point sArcPoint = picker.PickPoint("Укажите точку начала осевой дуге");
                    TSG.Point eArcPoint = picker.PickPoint("Укажите точку конца осевой дуге");
                    TSG.Point anyArcPoint = picker.PickPoint("Укажите точку на осевой дуге");

                    //Строим осевую дугу
                    TSM.ControlArc sCentralCArc = new TSM.ControlArc(sArcPoint, eArcPoint, anyArcPoint);
                    sCentralCArc.LineType = TSM.ControlObjectLineType.SolidLine;
                    sCentralCArc.Color = TSM.ControlObjectColorEnum.RED;
                    sCentralCArc.Insert();

                    //Параметры осевой дуги
                    ///Преобразование дуги из модели в геометрический объект
                    TSG.Arc sCentralArc = sCentralCArc.Geometry;

                    ///Параметры исходной осевой дуги
                    TSG.Point mCentralArcPoint = sCentralArc.ArcMiddlePoint;
                    TSG.Point cCentralArcPoint = sCentralArc.CenterPoint;
                    TSG.Vector normalCentralArc = sCentralArc.Normal;

                    double radiusCentralArc = Math.Round(sCentralArc.Radius, 10);
                    double lenghtCentraArc = Math.Round(sCentralArc.Length, 10);
                    double angleCentralArc = Math.Round(sCentralArc.Angle, 10);
                    double lenghtCentraArcDown = Math.Round(lenghtCentraArc + offsetBeam, 10);
                    double angleCentralArcDown = Math.Round(lenghtCentraArcDown / radiusCentralArc, 10);

                    //Начальная система координат
                    TSM.TransformationPlane origPlane = model.GetWorkPlaneHandler().GetCurrentTransformationPlane();
                    TSG.Matrix matrixOrigPlane = origPlane.TransformationMatrixToGlobal;

                    //Оффсет дуг верхнего пояса
                    ///Начальные вектора X и Y центральной дуги из точки начала дуги
                    TSG.Vector sCentralArcVX = sCentralArc.StartDirection;
                    TSG.Vector sCentralArcVY = sCentralArc.StartTangent;

                    ///Система координат в начале дуги
                    TSG.CoordinateSystem сArcStartPointCS = new TSG.CoordinateSystem(sArcPoint, sCentralArcVX, sCentralArcVY);
                    TSM.TransformationPlane cArcStartPlane = new TSM.TransformationPlane(сArcStartPointCS);
                    TSG.Matrix matrixStartPlane = cArcStartPlane.TransformationMatrixToGlobal;

                    ///Изменение системы координат
                    model.GetWorkPlaneHandler().SetCurrentTransformationPlane(cArcStartPlane);

                    ///Оффсет точек в начале дуги
                    TSG.Point sArcOffOutPoint = new TSG.Point(widthBeam / 2, 0.0, topThickness / 2); //внешняя точка верхнего пояса
                    TSG.Point sArcOffInPoint = new TSG.Point(-widthBeam / 2, 0.0, topThickness / 2); //внутренняя точка верхнего пояса

                    TSG.Point sArcOffOutPointDown = new TSG.Point(widthBeam / 2, 0.0, (heightWall+ topThickness) + topThickness / 2); //внешняя точка нижнего пояса
                    TSG.Point sArcOffInPointDown = new TSG.Point(-widthBeam / 2, 0.0, (heightWall + topThickness) + topThickness / 2); //внутренняя точка нижнего пояса

                    TSG.Point sArcOffOutPointWall1 = new TSG.Point(widthBeam / 2 - thicknessWall / 2, 0.0, topThickness); //внешняя точка стенки
                    TSG.Point sArcOffInPointWall1 = new TSG.Point(-widthBeam / 2 + thicknessWall / 2, 0.0, topThickness); //внутренняя точка стенки

                    TSG.Point sArcOffOutPointWall2 = new TSG.Point(widthBeam/2 - thicknessWall / 2, 0.0, heightWall + topThickness + topThickness + offsetWall); //внешняя точка стенки
                    TSG.Point sArcOffInPointWall2 = new TSG.Point(-widthBeam/2 + thicknessWall / 2, 0.0, heightWall + topThickness + topThickness + offsetWall); //внутренняя точка стенки

                    ///Вспомогательные точки, которые нужны
                    TSG.Point supPoint1 = new TSG.Point(widthBeam/2, 0.0, 0);
                    TSG.Point supPoint2 = new TSG.Point(-widthBeam/2, 0.0, 0);
                    TSG.Point supPoint3 = new TSG.Point(widthBeam/2, 0.0, topThickness);
                    TSG.Point supPoint4 = new TSG.Point(-widthBeam/2, 0.0, topThickness);
                    TSG.Point supPoint5 = new TSG.Point(widthBeam/2 - thicknessWall, 0.0, topThickness);
                    TSG.Point supPoint6 = new TSG.Point(-widthBeam/2 + thicknessWall, 0.0, topThickness);
                    TSG.Point supPoint7 = new TSG.Point(widthBeam/2, 0.0, heightWall + topThickness);
                    TSG.Point supPoint8 = new TSG.Point(-widthBeam/2, 0.0, heightWall + topThickness);
                    TSG.Point supPoint9 = new TSG.Point(widthBeam/2 - thicknessWall, 0.0, heightWall + topThickness);
                    TSG.Point supPoint10 = new TSG.Point(-widthBeam/2 + thicknessWall, 0.0, heightWall + topThickness);

                    ///Получаем глобальные координаты точек
                    TSG.Point sArcOffOutPointGl = matrixStartPlane.Transform(sArcOffOutPoint);
                    TSG.Point sArcOffInPointGl = matrixStartPlane.Transform(sArcOffInPoint);

                    TSG.Point sArcOffOutPointDownGl = matrixStartPlane.Transform(sArcOffOutPointDown);
                    TSG.Point sArcOffInPointDownGl = matrixStartPlane.Transform(sArcOffInPointDown);

                    TSG.Point sArcOffOutPointWall1Gl = matrixStartPlane.Transform(sArcOffOutPointWall1);
                    TSG.Point sArcOffInPointWall1Gl = matrixStartPlane.Transform(sArcOffInPointWall1);

                    TSG.Point sArcOffOutPointWall2Gl = matrixStartPlane.Transform(sArcOffOutPointWall2);
                    TSG.Point sArcOffInPointWall2Gl = matrixStartPlane.Transform(sArcOffInPointWall2);

                    supPoint1 = matrixStartPlane.Transform(supPoint1);
                    supPoint2 = matrixStartPlane.Transform(supPoint2);
                    supPoint3 = matrixStartPlane.Transform(supPoint3);
                    supPoint4 = matrixStartPlane.Transform(supPoint4);
                    supPoint5 = matrixStartPlane.Transform(supPoint5);
                    supPoint6 = matrixStartPlane.Transform(supPoint6);
                    supPoint7 = matrixStartPlane.Transform(supPoint7);
                    supPoint8 = matrixStartPlane.Transform(supPoint8);
                    supPoint9 = matrixStartPlane.Transform(supPoint9);
                    supPoint10 = matrixStartPlane.Transform(supPoint10);


                    TSM.ControlPoint sArcOffOutCPoint = new TSM.ControlPoint(sArcOffOutPointGl); //внешняя точка верхнего пояса
                    TSM.ControlPoint sArcOffInCPoint = new TSM.ControlPoint(sArcOffInPointGl); //внутренняя точка верхнего пояса

                    TSM.ControlPoint sArcOffOutCPointDown = new TSM.ControlPoint(sArcOffOutPointDownGl); //внешняя точка нижнего пояса
                    TSM.ControlPoint sArcOffInCPointDown = new TSM.ControlPoint(sArcOffInPointDownGl); //внутренняя точка нижнего пояса

                    TSM.ControlPoint sArcOffOutCPointWall1 = new TSM.ControlPoint(sArcOffOutPointWall1Gl); //внешняя точка стенки
                    TSM.ControlPoint sArcOffInCPointWall1 = new TSM.ControlPoint(sArcOffInPointWall1Gl); //внутренняя точка стенки

                    TSM.ControlPoint sArcOffOutCPointWall2 = new TSM.ControlPoint(sArcOffOutPointWall2Gl); //внешняя точка стенки
                    TSM.ControlPoint sArcOffInCPointWall2 = new TSM.ControlPoint(sArcOffInPointWall2Gl); //внутренняя точка стенки

                    TSM.ControlPoint supCPoint1 = new TSM.ControlPoint(supPoint1);
                    TSM.ControlPoint supCPoint2 = new TSM.ControlPoint(supPoint2);
                    TSM.ControlPoint supCPoint3 = new TSM.ControlPoint(supPoint3);
                    TSM.ControlPoint supCPoint4 = new TSM.ControlPoint(supPoint4);
                    TSM.ControlPoint supCPoint5 = new TSM.ControlPoint(supPoint5);
                    TSM.ControlPoint supCPoint6 = new TSM.ControlPoint(supPoint6);
                    TSM.ControlPoint supCPoint7 = new TSM.ControlPoint(supPoint7);
                    TSM.ControlPoint supCPoint8 = new TSM.ControlPoint(supPoint8);
                    TSM.ControlPoint supCPoint9 = new TSM.ControlPoint(supPoint9);
                    TSM.ControlPoint supCPoint10 = new TSM.ControlPoint(supPoint10);

                    model.GetWorkPlaneHandler().SetCurrentTransformationPlane(origPlane);

                    ///Вставляем точки в модель
                    sArcOffOutCPoint.Insert();
                    sArcOffInCPoint.Insert();

                    sArcOffOutCPointDown.Insert();
                    sArcOffInCPointDown.Insert();

                    sArcOffOutCPointWall1.Insert();
                    sArcOffInCPointWall1.Insert();

                    sArcOffOutCPointWall2.Insert();
                    sArcOffInCPointWall2.Insert();

                    supCPoint1.Insert();
                    supCPoint2.Insert();
                    supCPoint3.Insert();
                    supCPoint4.Insert();
                    supCPoint5.Insert();
                    supCPoint6.Insert();
                    supCPoint7.Insert();
                    supCPoint8.Insert();
                    supCPoint9.Insert();
                    supCPoint10.Insert();

                    //Строим дуги
                    TSG.Vector newVector = new TSG.Vector(0.0, 0.0, -1.0);

                    //Верхний пояс
                    TSG.Point cCentralArcPointOff = new TSG.Point(cCentralArcPoint.X, cCentralArcPoint.Y, sArcOffOutPointGl.Z);

                    TSG.Arc sArcOffOut = new TSG.Arc(cCentralArcPointOff, sArcOffOutPointGl, newVector, angleCentralArc);
                    TSG.Arc sArcOffIn = new TSG.Arc(cCentralArcPointOff, sArcOffInPointGl, newVector, angleCentralArc);

                    TSG.Point sArcOffOutMidPoint = sArcOffOut.ArcMiddlePoint;
                    TSG.Point sArcOffOutEndPoint = sArcOffOut.EndPoint;

                    TSG.Point sArcOffInMidPoint = sArcOffIn.ArcMiddlePoint;
                    TSG.Point sArcOffInEndPoint = sArcOffIn.EndPoint;

                    TSM.ControlArc sCArcOffOut = new TSM.ControlArc(sArcOffOutPointGl, sArcOffOutEndPoint, sArcOffOutMidPoint);
                    TSM.ControlArc sCArcOffIn = new TSM.ControlArc(sArcOffInPointGl, sArcOffInEndPoint, sArcOffInMidPoint);

                    ////Нижний пояс
                    TSG.Point cCentralArcPointOffDown = new TSG.Point(cCentralArcPoint.X, cCentralArcPoint.Y, sArcOffOutPointDownGl.Z);

                    TSG.Arc sArcOffOutDown = new TSG.Arc(cCentralArcPointOffDown, sArcOffOutPointDownGl, newVector, angleCentralArcDown);
                    TSG.Arc sArcOffInDown = new TSG.Arc(cCentralArcPointOffDown, sArcOffInPointDownGl, newVector, angleCentralArcDown);

                    TSG.Point sArcOffOutDownMidPoint = sArcOffOutDown.ArcMiddlePoint;
                    TSG.Point sArcOffOutDownEndPoint = sArcOffOutDown.EndPoint;

                    TSG.Point sArcOffInMidDownPoint = sArcOffInDown.ArcMiddlePoint;
                    TSG.Point sArcOffInEndDownPoint = sArcOffInDown.EndPoint;

                    TSM.ControlArc sCArcOffOutDown = new TSM.ControlArc(sArcOffOutPointDownGl, sArcOffOutDownEndPoint, sArcOffOutDownMidPoint);
                    TSM.ControlArc sCArcOffInDown = new TSM.ControlArc(sArcOffInPointDownGl, sArcOffInEndDownPoint, sArcOffInMidDownPoint);

                    ////Внешняя стенка
                    TSG.Point cCentralArcPointOutWall1 = new TSG.Point(cCentralArcPoint.X, cCentralArcPoint.Y, sArcOffOutPointWall1Gl.Z);
                    TSG.Point cCentralArcPointOutfWall2 = new TSG.Point(cCentralArcPoint.X, cCentralArcPoint.Y, sArcOffOutPointWall2Gl.Z);

                    TSG.Arc sArcOutWall1 = new TSG.Arc(cCentralArcPointOutWall1, sArcOffOutPointWall1Gl, newVector, angleCentralArc);
                    TSG.Arc sArcOutWall2 = new TSG.Arc(cCentralArcPointOutfWall2, sArcOffOutPointWall2Gl, newVector, angleCentralArc);

                    TSG.Point sArcOffOutWall1MidPoint = sArcOutWall1.ArcMiddlePoint;
                    TSG.Point sArcOffOutWall1EndPoint = sArcOutWall1.EndPoint;

                    TSG.Point sArcOffOutWall2MidPoint = sArcOutWall2.ArcMiddlePoint;
                    TSG.Point sArcOffOutWall2EndPoint = sArcOutWall2.EndPoint;

                    TSM.ControlArc sCArcOffOutWall1 = new TSM.ControlArc(sArcOffOutPointWall1Gl, sArcOffOutWall1EndPoint, sArcOffOutWall1MidPoint);
                    TSM.ControlArc sCArcOffOutWall2 = new TSM.ControlArc(sArcOffOutPointWall2Gl, sArcOffOutWall2EndPoint, sArcOffOutWall2MidPoint);

                    ////Внутренняя стенка
                    TSG.Point cCentralArcPointInWall1 = new TSG.Point(cCentralArcPoint.X, cCentralArcPoint.Y, sArcOffInPointWall1Gl.Z);
                    TSG.Point cCentralArcPointInfWall2 = new TSG.Point(cCentralArcPoint.X, cCentralArcPoint.Y, sArcOffInPointWall2Gl.Z);

                    TSG.Arc sArcInWall1 = new TSG.Arc(cCentralArcPointInWall1, sArcOffInPointWall1Gl, newVector, angleCentralArc);
                    TSG.Arc sArcInWall2 = new TSG.Arc(cCentralArcPointInfWall2, sArcOffInPointWall2Gl, newVector, angleCentralArc);

                    TSG.Point sArcOffInWall1MidPoint = sArcInWall1.ArcMiddlePoint;
                    TSG.Point sArcOffInWall1EndPoint = sArcInWall1.EndPoint;

                    TSG.Point sArcOffInWall2MidPoint = sArcInWall2.ArcMiddlePoint;
                    TSG.Point sArcOffInWall2EndPoint = sArcInWall2.EndPoint;

                    TSM.ControlArc sCArcOffInWall1 = new TSM.ControlArc(sArcOffInPointWall1Gl, sArcOffInWall1EndPoint, sArcOffInWall1MidPoint);
                    TSM.ControlArc sCArcOffInWall2 = new TSM.ControlArc(sArcOffInPointWall2Gl, sArcOffInWall2EndPoint, sArcOffInWall2MidPoint);

                    ///Вставка дуг
                    sCArcOffOut.LineType = ControlObjectLineType.SolidLine;
                    sCArcOffOut.Color = ControlObjectColorEnum.YELLOW;

                    sCArcOffIn.LineType = ControlObjectLineType.SolidLine;
                    sCArcOffIn.Color = ControlObjectColorEnum.YELLOW;

                    sCArcOffOutDown.LineType = ControlObjectLineType.SolidLine;
                    sCArcOffOutDown.Color = ControlObjectColorEnum.BLUE;

                    sCArcOffInDown.LineType = ControlObjectLineType.SolidLine;
                    sCArcOffInDown.Color = ControlObjectColorEnum.BLUE;

                    sCArcOffOutWall1.LineType = ControlObjectLineType.SolidLine;
                    sCArcOffOutWall1.Color = ControlObjectColorEnum.BLUE;

                    sCArcOffOutWall2.LineType = ControlObjectLineType.SolidLine;
                    sCArcOffOutWall2.Color = ControlObjectColorEnum.BLUE;

                    sCArcOffInWall1.LineType = ControlObjectLineType.SolidLine;
                    sCArcOffInWall1.Color = ControlObjectColorEnum.BLUE;

                    sCArcOffInWall2.LineType = ControlObjectLineType.SolidLine;
                    sCArcOffInWall2.Color = ControlObjectColorEnum.BLUE;

                    sCArcOffOut.Insert();
                    sCArcOffIn.Insert();

                    sCArcOffOutDown.Insert();
                    sCArcOffInDown.Insert();

                    sCArcOffOutWall1.Insert();
                    sCArcOffOutWall2.Insert();

                    sCArcOffInWall1.Insert();
                    sCArcOffInWall2.Insert();

                    //Вставляем лофтинговые пластины
                    ///Верхний пояс
                    var curvesTop = new List<ICurve>();
                    curvesTop.Add(sCArcOffOut.Geometry);
                    curvesTop.Add(sCArcOffIn.Geometry);

                    TSM.LoftedPlate loftedPlateTop1 = new TSM.LoftedPlate
                    {
                        BaseCurves = curvesTop,
                        Name = "Верхний пояс",
                        Class = classTop,
                    };

                    ///Нижний пояс
                    var curvesDown = new List<ICurve>();
                    curvesDown.Add(sCArcOffOutDown.Geometry);
                    curvesDown.Add(sCArcOffInDown.Geometry);

                    TSM.LoftedPlate loftedPlateDown1 = new TSM.LoftedPlate
                    {
                        BaseCurves = curvesDown,
                        Name = "Нижний пояс",
                        Class = classBottom,
                    };

                    ///Внешняя стенка
                    var curvesOutWall1 = new List<ICurve>();
                    curvesOutWall1.Add(sCArcOffOutWall1.Geometry);
                    curvesOutWall1.Add(sCArcOffOutWall2.Geometry);

                    TSM.LoftedPlate loftedPlateWallOut1 = new TSM.LoftedPlate
                    {
                        BaseCurves = curvesOutWall1,
                        Name = "Стенка",
                        Class = classWall,
                    };

                    ///Внутренняя стенка
                    var curvesInWall1 = new List<ICurve>();
                    curvesInWall1.Add(sCArcOffInWall1.Geometry);
                    curvesInWall1.Add(sCArcOffInWall2.Geometry);

                    TSM.LoftedPlate loftedPlateWallIn1 = new TSM.LoftedPlate
                    {
                        BaseCurves = curvesInWall1,
                        Name = "Стенка",
                        Class = classWall,
                    };

                    var partNumber = new TSM.NumberingSeries();
                    var assemblyNumber = new TSM.NumberingSeries();
                    partNumber.Prefix = prefixDetail;
                    partNumber.StartNumber = 1;
                    assemblyNumber.Prefix = prefixAssembly;
                    assemblyNumber.StartNumber = 1;

                    loftedPlateTop1.Profile.ProfileString = "PL" + topThickness.ToString();
                    loftedPlateTop1.Material.MaterialString = material;
                    loftedPlateTop1.PartNumber = partNumber;
                    loftedPlateTop1.AssemblyNumber = assemblyNumber;

                    loftedPlateDown1.Profile.ProfileString = "PL" + topThickness.ToString();
                    loftedPlateDown1.Material.MaterialString = material;
                    loftedPlateDown1.PartNumber = partNumber;
                    loftedPlateDown1.AssemblyNumber = assemblyNumber;

                    loftedPlateWallOut1.Profile.ProfileString = "PL" + thicknessWall.ToString();
                    loftedPlateWallOut1.Material.MaterialString = material;
                    loftedPlateWallOut1.PartNumber = partNumber;
                    loftedPlateWallOut1.AssemblyNumber = assemblyNumber;

                    loftedPlateWallIn1.Profile.ProfileString = "PL" + thicknessWall.ToString();
                    loftedPlateWallIn1.Material.MaterialString = material;
                    loftedPlateWallIn1.PartNumber = partNumber;
                    loftedPlateWallIn1.AssemblyNumber = assemblyNumber;

                    loftedPlateTop1.Insert();
                    loftedPlateDown1.Insert();
                    loftedPlateWallOut1.Insert();
                    loftedPlateWallIn1.Insert();

                    //Копирование пластин
                    var copyVectorTop = new TSG.Vector(0, 0, -(heightAssembly - heightWall - 2 * topThickness));
                    var copyVectorWall = new TSG.Vector(copyVectorTop.X, copyVectorTop.Y, copyVectorTop.Z + offsetWall + topThickness);
                    var copyVectorWallPoint = new TSG.Vector(copyVectorTop.X, copyVectorTop.Y, copyVectorTop.Z + offsetWall + topThickness);

                    TSM.LoftedPlate loftedPlateTop2 = TSMOP.Operation.CopyObject(loftedPlateTop1, copyVectorTop) as TSM.LoftedPlate;
                    TSM.LoftedPlate loftedPlateDown2 = TSMOP.Operation.CopyObject(loftedPlateDown1, copyVectorTop) as TSM.LoftedPlate;
                    TSM.LoftedPlate loftedPlateWallOut2 = TSMOP.Operation.CopyObject(loftedPlateWallOut1, copyVectorWall) as TSM.LoftedPlate;
                    TSM.LoftedPlate loftedPlateWallIn2 = TSMOP.Operation.CopyObject(loftedPlateWallIn1, copyVectorWall) as TSM.LoftedPlate;

                    loftedPlateTop2.Name = "Верхний пояс";
                    loftedPlateTop2.Class = classTop;
                    loftedPlateTop2.Modify();

                    loftedPlateDown2.Name = "Нижний пояс";
                    loftedPlateDown2.Class = classBottom;
                    loftedPlateDown2.Modify();

                    loftedPlateWallOut2.Name = "Стенка";
                    loftedPlateWallOut2.Class = classWall;
                    loftedPlateWallOut2.Modify();

                    loftedPlateWallIn2.Name = "Стенка";
                    loftedPlateWallIn2.Class = classWall;
                    loftedPlateWallIn2.Modify();

                    //Копирование вспомогательных точек
                    TSM.ControlPoint supCPoint1Copy = TSMOP.Operation.CopyObject(supCPoint1, copyVectorTop) as TSM.ControlPoint;
                    TSM.ControlPoint supCPoint2Copy = TSMOP.Operation.CopyObject(supCPoint2, copyVectorTop) as TSM.ControlPoint;
                    TSM.ControlPoint supCPoint3Copy = TSMOP.Operation.CopyObject(supCPoint3, copyVectorTop) as TSM.ControlPoint;
                    TSM.ControlPoint supCPoint4Copy = TSMOP.Operation.CopyObject(supCPoint4, copyVectorTop) as TSM.ControlPoint;
                    TSM.ControlPoint supCPoint5Copy = TSMOP.Operation.CopyObject(supCPoint5, copyVectorTop) as TSM.ControlPoint;
                    TSM.ControlPoint supCPoint6Copy = TSMOP.Operation.CopyObject(supCPoint6, copyVectorTop) as TSM.ControlPoint;
                    TSM.ControlPoint supCPoint7Copy = TSMOP.Operation.CopyObject(supCPoint7, copyVectorTop) as TSM.ControlPoint;
                    TSM.ControlPoint supCPoint8Copy = TSMOP.Operation.CopyObject(supCPoint8, copyVectorTop) as TSM.ControlPoint;
                    TSM.ControlPoint supCPoint9Copy = TSMOP.Operation.CopyObject(supCPoint9, copyVectorTop) as TSM.ControlPoint;
                    TSM.ControlPoint supCPoint10Copy = TSMOP.Operation.CopyObject(supCPoint10, copyVectorTop) as TSM.ControlPoint;

                    //Создание сварки
                    TSM.Weld myWeld = new TSM.Weld();
                    myWeld.MainObject = loftedPlateWallIn1;
                    myWeld.SecondaryObject = loftedPlateTop1;
                    myWeld.ShopWeld = true;
                    myWeld.Insert();

                    myWeld.SecondaryObject = loftedPlateDown1;
                    myWeld.Insert();

                    myWeld.SecondaryObject = loftedPlateWallOut1;
                    myWeld.Insert();

                    myWeld.SecondaryObject = loftedPlateWallIn2;
                    myWeld.Insert();

                    myWeld.SecondaryObject = loftedPlateTop2;
                    myWeld.Insert();

                    myWeld.SecondaryObject = loftedPlateDown2;
                    myWeld.Insert();

                    myWeld.SecondaryObject = loftedPlateWallOut2;
                    myWeld.Insert();

                    //Удаление вспомогательных объектов
                    sArcOffOutCPoint.Delete();
                    sArcOffInCPoint.Delete();

                    sArcOffOutCPointDown.Delete();
                    sArcOffInCPointDown.Delete();

                    sArcOffOutCPointWall1.Delete();
                    sArcOffInCPointWall1.Delete();

                    sArcOffOutCPointWall2.Delete();
                    sArcOffInCPointWall2.Delete();

                    sCArcOffOut.Delete();
                    sCArcOffIn.Delete();

                    sCArcOffOutDown.Delete();
                    sCArcOffInDown.Delete();

                    sCArcOffOutWall1.Delete();
                    sCArcOffOutWall2.Delete();

                    sCArcOffInWall1.Delete();
                    sCArcOffInWall2.Delete();

                    model.CommitChanges();

                    while (viewEnum.MoveNext())
                    {
                        View currentView = viewEnum.Current;
                        ViewHandler.RedrawView(currentView);
                    }

                }

                catch (Exception errorMessage)
                {
                    MessageBox.Show(errorMessage.ToString());
                }
            }
        }

        private void ButtonCreateComponent_Click(object sender, RoutedEventArgs e)
        {
            model = model ?? new TSM.Model();

            if (!model.GetConnectionStatus())
            {
                MessageBox.Show("Не удалось подключиться к Tekla Structures");
            }

            else
            {
                try
                {
                    Picker picker = new TSMUI.Picker();

                    //Перечислитель активных видов
                    ModelViewEnumerator viewEnum = ViewHandler.GetVisibleViews();

                    //Исходная рабочая плоскость
                    TSM.TransformationPlane originPlane = model.GetWorkPlaneHandler().GetCurrentTransformationPlane();

                    TSG.Point centerPoint = picker.PickPoint("Укажите центральную точку");
                    ArrayList inputPoints = picker.PickPoints(Picker.PickPointEnum.PICK_POLYGON,"Укажите точки осей стоек");


                    for (int i = 0; i < inputPoints.Count; i++)
                    {
                        //Параметры создаваемого компонента
                        TSM.Component newComponent = new TSM.Component();
                        newComponent.Name = ComponentName.Text;
                        newComponent.Number = -1;

                        TSM.ComponentInput componentInput = new TSM.ComponentInput();

                        //Изменение системы координат и рабочей плоскости для вставки стоек
                        TSG.Point inputPoint = inputPoints[i] as TSG.Point;
                        TSG.Line inputLine = new TSG.Line(centerPoint, inputPoint);
                        TSG.Vector vectorX = inputLine.Direction.GetNormal();
                        TSG.Vector vectorY = new TSG.Vector(-vectorX.Y, vectorX.X, vectorX.Z);
                        TSG.CoordinateSystem newCS = new TSG.CoordinateSystem(inputPoint, vectorX, vectorY);
                        TSM.TransformationPlane newPlane = new TSM.TransformationPlane(newCS);
                        model.GetWorkPlaneHandler().SetCurrentTransformationPlane(newPlane);

                        while (viewEnum.MoveNext())
                        {
                            View currentView = viewEnum.Current;
                            ViewHandler.RedrawView(currentView);
                        }

                        //Вставка смещенных точек для вставки компонента
                        TSG.Point offsetInPoint = new TSG.Point(-150.0, 0.0, 0.0);
                        TSG.Point offsetOutPoint = new TSG.Point(150.0, 0.0, 0.0);
                        TSM.ControlPoint offsetInPointModel = new ControlPoint(offsetInPoint);
                        TSM.ControlPoint offsetOutPointModel = new ControlPoint(offsetOutPoint);

                        componentInput.AddOneInputPosition(offsetInPoint);
                        componentInput.AddOneInputPosition(offsetOutPoint);

                        newComponent.SetComponentInput(componentInput);
                        if (!newComponent.Insert())
                        {
                           MessageBox.Show("Не удалось вставить компонент");
                        }

                        model.GetWorkPlaneHandler().SetCurrentTransformationPlane(originPlane);

                        while (viewEnum.MoveNext())
                        {
                            View currentView = viewEnum.Current;
                            ViewHandler.RedrawView(currentView);
                        }

                    }

                    model.CommitChanges();

                }

                catch (Exception errorMessage)
                {
                    MessageBox.Show(errorMessage.ToString());
                }
            }
            }


        private void ButtonResetParametersTab1_Click(object sender, RoutedEventArgs e)
        {
            HeightAssembly.Text = "1650";
            WidthBeam.Text = "300";
            TopThickness.Text = "20";
            OffsetBeam.Text = "50";
            HeightWall.Text = "360";
            ThicknessWall.Text = "25";
            OffsetWall.Text = "150";

            ClassTop.Text = "1";
            ClassBottom.Text = "4";
            ClassWall.Text = "10";
            Material.Text = "С390-1-6";
            PrefixDetail.Text = "515-6.";
            PrefixAssembly.Text = "515-6.ЯФ";
            ComponentName.Text = "";

        }


        private void HeightAssembly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            double val;
            if (!Double.TryParse(e.Text, out val) && e.Text != ".")
            {
                e.Handled = true; // отклоняем ввод
            }
        }

        private void WidthBeam_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            double val;
            if (!Double.TryParse(e.Text, out val) && e.Text != ".")
            {
                e.Handled = true; // отклоняем ввод
            }
        }

        private void TopThickness_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            double val;
            if (!Double.TryParse(e.Text, out val) && e.Text != ".")
            {
                e.Handled = true; // отклоняем ввод
            }
        }

        private void OffsetBeam_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            double val;
            if (!Double.TryParse(e.Text, out val) && e.Text != ".")
            {
                e.Handled = true; // отклоняем ввод
            }
        }

        private void HeightWall_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            double val;
            if (!Double.TryParse(e.Text, out val) && e.Text != ".")
            {
                e.Handled = true; // отклоняем ввод
            }
        }

        private void ThicknessWall_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            double val;
            if (!Double.TryParse(e.Text, out val) && e.Text != ".")
            {
                e.Handled = true; // отклоняем ввод
            }
        }

        private void OffsetWall_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            double val;
            if (!Double.TryParse(e.Text, out val) && e.Text != ".")
            {
                e.Handled = true; // отклоняем ввод
            }
        }

        private void ClassTop_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int val;
            if (!Int32.TryParse(e.Text, out val))
            {
                e.Handled = true; // отклоняем ввод
            }
        }

        private void ClassBottom_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int val;
            if (!Int32.TryParse(e.Text, out val))
            {
                e.Handled = true; // отклоняем ввод
            }
        }

        private void ClassWall_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int val;
            if (!Int32.TryParse(e.Text, out val))
            {
                e.Handled = true; // отклоняем ввод
            }
        }


    }
}
