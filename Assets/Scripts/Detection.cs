﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using UnityEngine.UI;


public class Detection : MonoBehaviour
{
    private VideoCapture _webcam;
    
    public RawImage display;
    public Texture2D texture;


    public double hShieldMin;
    public double sShieldMin;
    public double vShieldMin;

    public double hShieldMax = 179;
    public double sShieldMax = 255;
    public double vShieldMax = 255;

    public double hSwordMin;
    public double sSwordMin;
    public double vSwordMin;

    public double hSwordMax = 179;
    public double sSwordMax = 255;
    public double vSwordMax = 255;

    public int operationSize = 1;
    public int nbrIteration = 2;

    private VectorOfPoint shieldContour;

    private VectorOfPoint swordContour;

    private Mat image;

    // Start is called before the first frame update
    void Start()
    {
        _webcam = new VideoCapture(0);

    }

    // Update is called once per frame
    void Update()
    {
        

        //Query the frame from the webcam
        image = _webcam.QueryFrame();
        CvInvoke.Flip(image, image, Emgu.CV.CvEnum.FlipType.Vertical);

        ProcessImage(image);

        DisplayFrame(image);


    }

    
    private Mat ProcessImage(Mat frame)
    {
        Mat flippedImage = frame.Clone();

      


        //Focus on the shield
        Mat frameHsvMat = new Mat();
        CvInvoke.CvtColor(flippedImage,frameHsvMat,ColorConversion.Bgr2Hsv);

        Image<Hsv, byte> frameHSV = frameHsvMat.ToImage<Hsv, byte>();

        //Detection of the shield
        Hsv lowerShield = new Hsv(hShieldMin, sShieldMin, vShieldMin);
        Hsv upperShield = new Hsv(hShieldMax, sShieldMax, vShieldMax);

       
        Mat frameShield = frameHSV.InRange(lowerShield, upperShield).Mat;

        Mat structuringElement = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(2 * operationSize + 1, 2 * operationSize + 1), new Point(operationSize, operationSize) );

        CvInvoke.Erode(frameShield, frameShield, structuringElement, new Point(-1, -1), nbrIteration, BorderType.Constant, new MCvScalar(0));
        CvInvoke.Erode(frameShield, frameShield, structuringElement, new Point(-1, -1), nbrIteration, BorderType.Constant, new MCvScalar(0));

        FindContour(frameShield,"Shield");

        //Detection of the sword

        Hsv lowerSword = new Hsv(hSwordMin,sSwordMin,vSwordMin);
        Hsv upperSword = new Hsv(hSwordMax,sSwordMax,vSwordMax);

        Mat frameSword = frameHSV.InRange(lowerSword, upperSword).Mat;

        CvInvoke.Erode(frameSword, frameSword, structuringElement, new Point(-1, -1), nbrIteration, BorderType.Constant, new MCvScalar(0));
        CvInvoke.Erode(frameSword, frameSword, structuringElement, new Point(-1, -1), nbrIteration, BorderType.Constant, new MCvScalar(0));

        FindContour(frameSword,"Sword");

        //Focus on the sword
        return frameShield;
    }

    private void FindContour(Mat frame,String nameObj)
    {
        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
        VectorOfPoint biggestContour = new VectorOfPoint();
        int biggestContourIndex = -1;
        double biggestContourArea;

        Mat hierarchy = new Mat();
        CvInvoke.FindContours(frame,contours,hierarchy, RetrType.List, ChainApproxMethod.ChainApproxNone);

        //gather the one with the bggest areas and its properties;
        for (int i = 0; i < contours.Size; i++)
        {
            biggestContour = contours[i];
            biggestContourIndex = i;
            biggestContourArea = CvInvoke.ContourArea(contours[i]);
        }

        if (biggestContourIndex > -1)
        {
            if (nameObj == "Shield" )
                shieldContour = biggestContour;
            else if (nameObj == "Sword")
                swordContour = biggestContour;

            CvInvoke.DrawContours(image,contours,biggestContourIndex,new MCvScalar(0,0,255));
        }
    }
    private void DisplayFrame(Mat frame)
    {
        if (!frame.IsEmpty)
        {
            if (frame.IsContinuous)
            {
                int width = (int)display.rectTransform.rect.width;
                int height = (int)display.rectTransform.rect.height;

                if (texture != null)
                {
                    Destroy(texture);
                    texture = null;
                }

                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                CvInvoke.Resize(frame, frame, new Size(width, height));
                CvInvoke.CvtColor(frame, frame, ColorConversion.Bgr2Rgba);

                texture.LoadRawTextureData(frame.ToImage<Rgba, Byte>().Bytes);
                texture.Apply();

                display.texture = texture;
            }
        }
    }
}
