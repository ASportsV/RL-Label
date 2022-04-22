using System;
using System.Drawing;
using UnityEngine;

public struct Vector
{

	private double mMagnitude;
	private double mDirection;

	public double Magnitude
	{
		get { return mMagnitude; }
		set { mMagnitude = value; }
	}

	public double Direction
	{
		get { return mDirection; }
		set { mDirection = value; }
	}

	public Vector(double magnitude, double direction)
	{
		mMagnitude = magnitude;
		mDirection = direction;

		if (mMagnitude < 0)
		{
			mMagnitude = -mMagnitude;
			mDirection = (180.0 + mDirection) % 360;
		}

		if (mDirection < 0) mDirection = (360.0 + mDirection);
	}

	public static Vector operator +(Vector a, Vector b)
	{
		//double aX = a.Magnitude * Math.Cos((Math.PI / 180.0) * a.Direction);
		//double aY = a.Magnitude * Math.Sin((Math.PI / 180.0) * a.Direction);

		//double bX = b.Magnitude * Math.Cos((Math.PI / 180.0) * b.Direction);
		//double bY = b.Magnitude * Math.Sin((Math.PI / 180.0) * b.Direction);

		//aX += bX;
		//aY += bY;

		Vector2 total = a.ToPoint() + b.ToPoint();
		double magnitude = Math.Sqrt(Math.Pow(total.x, 2) + Math.Pow(total.y, 2));

		double direction;
		if (magnitude == 0)
			direction = 0;
		else
			direction = (180.0 / Math.PI) * Math.Atan2(total.y, total.x);

		return new Vector(magnitude, direction);
	}

	public static Vector operator *(Vector vector, double multiplier)
	{
		return new Vector(vector.Magnitude * multiplier, vector.Direction);
	}

	public Vector2 ToPoint()
	{
		// break into x-y components
		double aX = mMagnitude * Math.Cos((Math.PI / 180.0) * mDirection);
		double aY = mMagnitude * Math.Sin((Math.PI / 180.0) * mDirection);

		return new Vector2((float)aX, (float)aY);
	}

	public override string ToString()
	{
		return mMagnitude.ToString("N5") + " " + mDirection.ToString("N2") + "°";
	}
}