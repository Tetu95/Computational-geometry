using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//A collection of classes to make the methods more general
namespace Habrador_Computational_Geometry
{
    //Bezier with two handles
    public class BezierCubic : _Curve
    {
        //Start and end points
        public MyVector3 posA;
        public MyVector3 posB;
        //Handles connected to the start and end points
        public MyVector3 handlePosA;
        public MyVector3 handlePosB;


        public BezierCubic(MyVector3 posA, MyVector3 posB, MyVector3 handleA, MyVector3 handleB)
        {
            this.posA = posA;
            this.posB = posB;

            this.handlePosA = handleA;
            this.handlePosB = handleB;
        }


        //
        // Position on the curve at point t
        //

        public override MyVector3 GetPosition(float t)
        {
            MyVector3 interpolatedValue = GetPosition(posA, posB, handlePosA, handlePosB, t);

            return interpolatedValue;
        }

        //Uses de Casteljau's algorithm 
        public static MyVector3 GetPosition(MyVector3 posA, MyVector3 posB, MyVector3 handlePosA, MyVector3 handlePosB, float t)
        {
            //MyVector3 interpolation_1 = BezierLinear(posA, handlePosA, t);
            //MyVector3 interpolation_2 = BezierLinear(handlePosA, handlePosB, t);
            //MyVector3 interpolation_3 = BezierLinear(handlePosB, posB, t);

            //MyVector3 interpolation_1_2 = BezierLinear(interpolation_1, interpolation_2, t);
            //MyVector3 interpolation_2_3 = BezierLinear(interpolation_2, interpolation_3, t);


            //Above can be simplified if we are utilizing the quadratic bezier
            //MyVector3 interpolation_1_2 = BezierQuadratic.GetPosition(posA, handlePosB, handlePosA, t);
            //MyVector3 interpolation_2_3 = BezierQuadratic.GetPosition(handlePosA, posB, handlePosB, t);

            //MyVector3 finalInterpolation = BezierLinear.GetPosition(interpolation_1_2, interpolation_2_3, t);


            //Above can be simplified by putting it into one big equation:
            //Layer 1
            //(1-t)A + tB = A - At + Bt
            //(1-t)B + tC = B - Bt + Ct
            //(1-t)C + tD = C - Ct + Dt

            //Layer 2
            //(1-t)(A - At + Bt) + t(B - Bt + Ct) = A - At + Bt - At + At^2 - Bt^2 + Bt - Bt^2 + Ct^2 = A - 2At + 2Bt + At^2 - 2Bt^2 + Ct^2 
            //(1-t)(B - Bt + Ct) + t(C - Ct + Dt) = B - Bt + Ct - Bt + Bt^2 - Ct^2 + Ct - Ct^2 + Dt^2 = B - 2Bt + 2Ct + Bt^2 - 2Ct^2 + Dt^2

            //Layer 3
            //(1-t)(A - 2At + 2Bt + At^2 - 2Bt^2 + Ct^2) + t(B - 2Bt + 2Ct + Bt^2 - 2Ct^2 + Dt^2)
            //A - 2At + 2Bt + At^2 - 2Bt^2 + Ct^2 - At + 2At^2 - 2Bt^2 - At^3 + 2Bt^3 - Ct^3 + Bt - 2Bt^2 + 2Ct^2 + Bt^3 - 2Ct^3 + Dt^3
            //A - 3At + 3Bt + 3At^2 - 6Bt^2 + 3Ct^2 - At^3 + 3Bt^3 - 3Ct^3 + Dt^3
            //A - 3t(A - B) + t^2(3A - 6B + 3C) + t^3(-A + 3B - 3C + D)
            //A - 3t(A - B) + t^2(3(A - 2B + C)) + t^3(-(A - 3(B - C) - D)

            MyVector3 A = posA;
            MyVector3 B = handlePosA;
            MyVector3 C = handlePosB;
            MyVector3 D = posB;

            MyVector3 finalInterpolation = A;

            finalInterpolation += -t * (3f * (A - B));

            finalInterpolation += Mathf.Pow(t, 2f) * (3f * (A - 2f * B + C));
            
            //t^3 -> cubic 
            finalInterpolation += Mathf.Pow(t, 3f) * (-(A - 3f * (B - C) - D));

            return finalInterpolation;
        }



        //
        // Forward direction on the curve at point t
        //

        //This direction is always tangent to the curve
        public static MyVector3 GetForwardDir(MyVector3 posA, MyVector3 posB, MyVector3 handlePosA, MyVector3 handlePosB, float t)
        {
            //Alternative 1
            //Same as when we calculate position from t
            //MyVector3 interpolation_1_2 = BezierQuadratic.GetPosition(posA, handlePosB, handlePosA, t);
            //MyVector3 interpolation_2_3 = BezierQuadratic.GetPosition(handlePosA, posB, handlePosB, t);

            //MyVector3 forwardDir = MyVector3.Normalize(interpolation_2_3 - interpolation_1_2);

            //Alternative 2
            //The forward dir is also the derivative vector
            MyVector3 forwardDir = MyVector3.Normalize(ExactDerivativeVec(posA, posB, handlePosA, handlePosB, t));

            return forwardDir;
        }

        public MyVector3 GetForwardDir(float t)
        {
            MyVector3 forwardDir = GetForwardDir(posA, posB, handlePosA, handlePosB, t);

            return forwardDir;
        }



        //
        // Derivative on the curve at point t
        //

        public override float GetDerivative(float t)
        {
            //Alternative 1
            //float derivative = InterpolationHelpMethods.EstimateDerivative(this, t);

            //Alternative 2
            float derivative = ExactDerivative(t);

            return derivative;
        }

        public static MyVector3 ExactDerivativeVec(MyVector3 posA, MyVector3 posB, MyVector3 handlePosA, MyVector3 handlePosB, float t)
        {
            MyVector3 A = posA;
            MyVector3 B = handlePosA;
            MyVector3 C = handlePosB;
            MyVector3 D = posB;

            //The derivative of the equation we use when finding position along the curve at t: 
            //-3(A - B) + 2t(3(A - 2B + C)) + 3t^2(-(A - 3(B - C) - D)
            //-3(A - B) + t(6(A - 2B + C)) + t^2(-3(A - 3(B - C) - D)

            MyVector3 derivativeVector = -3f * (A - B);

            derivativeVector += t * (6f * (A - 2f * B + C));

            derivativeVector += t * t * (-3f * (A - 3f * (B - C) - D));

            return derivativeVector;
        }

        public float ExactDerivative(float t)
        {
            MyVector3 derivativeVec = ExactDerivativeVec(posA, posB, handlePosA, handlePosB, t);

            float derivative = MyVector3.Magnitude(derivativeVec);

            return derivative;
        }



        //
        // Transform (position and orientation) at point t
        //

        public InterpolationTransform GetTransform(float t)
        {
            //The position on the curve at point t
            MyVector3 pos = GetPosition(t);

            //This forward direction (tangent) on the curve at point t
            MyVector3 forwardDir = GetForwardDir(posA, posB, handlePosA, handlePosB, t);


            //The position and the tangent are easy to find, what's difficult to find is the normal because a line doesn't have a single normal

            //To get the normal in 2d, we can just flip two coordinates in the forward vector and set one to negative
            //MyVector3 normal = new MyVector3(-forwardDir.z, 0f, forwardDir.x);


            //In 3d there are multiple alternatives

            //Alternative 1

            //A simple way to get the other directions is to use LookRotation with just forward dir as parameter
            //Then the up direction will always be the world up direction, and it calculates the right direction 
            //This idea is not working for all possible curve orientations
            MyQuaternion orientation = new MyQuaternion(forwardDir);

            //This is the same as providing a reference vector which is up
            //Quaternion orientation = InterpolationTransform.GetOrientationByUsingUpRef(forwardDir, Vector3.up.ToMyVector3());


            InterpolationTransform trans = new InterpolationTransform(pos, orientation);

            return trans;
        }
    }
}