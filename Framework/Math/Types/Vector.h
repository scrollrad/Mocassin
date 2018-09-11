//////////////////////////////////////////
// Project: C Monte Carlo Simulator		//
// File:	Vector.h        		    //
// Author:	Sebastian Eisele			//
//			Workgroup Martin, IPC       //
//			RWTH Aachen University      //
//			© 2018 Sebastian Eisele     //
// Short:   3D, 4D vector math support  //
//////////////////////////////////////////

#pragma once
#include <stdint.h>
#include <immintrin.h>

// Defines 4 component int32_t sse2 vector type with 4x4 bytes
typedef int32_t sse2v4i __attribute__((vector_size(16)));

// Defines the 192 bit 3D double vector struct and the affiliated type (Does not directly support 16 bit alignment)
typedef struct { double a, b, c; } vector3_t;

// Defines the 128 bit 4D int vector struct and the affiliated type (Supports 16 bit alignment)
typedef struct { int32_t a, b, c, d; } vector4_t;

// Performs a vector addition (a + b) and returns the resulting vector
vector3_t AddVector3(const vector3_t* restrict lhs, const vector3_t* restrict rhs);

// Performs a vector substraction (a - b) and returns the resulting vector
vector3_t SubstractVector3(const vector3_t* lhs, const vector3_t* rhs);

// Performs a multiplication of the vector with a scalar and returns the resulting vector
vector3_t ScalarMultVector3(const vector3_t* lhs, double rhs);

// Performs a divion of the vector with a scalar and returns the resulting vector
vector3_t ScalarDivideVector3(const vector3_t* lhs, double rhs);

// Performs a dot product claculation (a * b) and returns the result
double CalcVector3DotProduct(const vector3_t* lhs, const vector3_t* rhs);

// Performs a cross product calculation (a x b) and returns the resulting vector
vector3_t CalcVector3CrossProduct(const vector3_t* lhs, const vector3_t* rhs);

// Performs a spat product calculation (a * (b x c)) and returns the resulting value (Directional volume)
double CalcVector3SpatProduct(const vector3_t* vec_a, const vector3_t* vec_b, const vector3_t * vec_c);


// Performs a vector addition (a + b) and retruns the resulting int vector
vector4_t AddVector4(const vector4_t* lhs, const vector4_t* rhs);

// Performs a vector substraction (a - b) and retruns the resulting int vector
vector4_t SubstractVector4(const vector4_t* lhs, const vector4_t* rhs);

// Performs a multiplication of the int vector with a scalar and returns the resulting vector
vector4_t ScalarMultVector4(const vector4_t* lhs, int32_t rhs);

// Performs a divion of the vector with a scalar and returns the resulting vector
vector4_t ScalarDivideVector4(const vector4_t* lhs, int32_t rhs);

// Performs a conversion of a 4d int vector into an size_t value with the provided 4d size information (4D coordinate decoding by block sizes)
int32_t Int32FromVector4(const vector4_t* restrict value, const int32_t* restrict blockSizes);

// Performs conversion of a 4d int vector pair describing start and relative offset into a linear id (4D coordinate decoding by block sizes)
int32_t Int32FromVector4Pair(const vector4_t* restrict start, const vector4_t* restrict offset, const int32_t* restrict blockSizes);

// Performs a conversion of a size_t value into a 4d int vector with the provided 4d size information (4D coordinate encoding by block sizes)
vector4_t Vector4FromInt32(int32_t value, const int32_t* restrict blockSizes);

// Performs a periodic trim of a 4d integer vector with the provided sizes (Loop based, faster than modulo due to rare occurence of actual required trim)
static inline void PeriodicTrimVector4(vector4_t* restrict vector, const vector4_t* restrict sizes)
{
    while (vector->a <  0) vector->a += sizes->a;
    while (vector->a >= sizes->a) vector->a -= sizes->a;
    while (vector->b <  0) vector->b += sizes->b;
    while (vector->b >= sizes->b) vector->b -= sizes->b;
    while (vector->c <  0) vector->c += sizes->c;
    while (vector->c >= sizes->c) vector->c -= sizes->c;
}