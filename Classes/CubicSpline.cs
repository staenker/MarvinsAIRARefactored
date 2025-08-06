
namespace MarvinsAIRARefactored.Classes;

public class CubicSpline
{
	private readonly float[] _a, _b, _c, _d, _x;

	public CubicSpline( float[] x, float[] y )
	{
		var n = x.Length;

		_x = x;

		_a = new float[ n ];
		_b = new float[ n - 1 ];
		_c = new float[ n ];
		_d = new float[ n - 1 ];

		Array.Copy( y, _a, n );

		var h = new float[ n - 1 ];

		for ( var i = 0; i < n - 1; i++ )
		{
			h[ i ] = x[ i + 1 ] - x[ i ];
		}

		var alpha = new float[ n ];

		for ( var i = 1; i < n - 1; i++ )
		{
			alpha[ i ] = ( 3f / h[ i ] ) * ( _a[ i + 1 ] - _a[ i ] ) - ( 3f / h[ i - 1 ] ) * ( _a[ i ] - _a[ i - 1 ] );
		}

		var l = new float[ n ];
		var mu = new float[ n ];
		var z = new float[ n ];

		l[ 0 ] = 1f;
		mu[ 0 ] = 0f;
		z[ 0 ] = 0f;

		for ( var i = 1; i < n - 1; i++ )
		{
			l[ i ] = 2f * ( x[ i + 1 ] - x[ i - 1 ] ) - h[ i - 1 ] * mu[ i - 1 ];
			mu[ i ] = h[ i ] / l[ i ];
			z[ i ] = ( alpha[ i ] - h[ i - 1 ] * z[ i - 1 ] ) / l[ i ];
		}

		l[ n - 1 ] = 1;
		z[ n - 1 ] = 0;
		_c[ n - 1 ] = 0;

		for ( var j = n - 2; j >= 0; j-- )
		{
			_c[ j ] = z[ j ] - mu[ j ] * _c[ j + 1 ];
			_b[ j ] = ( _a[ j + 1 ] - _a[ j ] ) / h[ j ] - h[ j ] * ( _c[ j + 1 ] + 2f * _c[ j ] ) / 3f;
			_d[ j ] = ( _c[ j + 1 ] - _c[ j ] ) / ( 3f * h[ j ] );
		}
	}

	public float Interpolate( float xValue )
	{
		var i = Array.BinarySearch( _x, xValue );

		if ( i < 0 )
		{
			i = ~i - 1;
		}

		if ( i < 0 )
		{
			i = 0;
		}
		else if ( i >= _x.Length - 1 )
		{
			i = _x.Length - 2;
		}

		var dx = xValue - _x[ i ];

		return _a[ i ] + _b[ i ] * dx + _c[ i ] * dx * dx + _d[ i ] * dx * dx * dx;
	}
}
