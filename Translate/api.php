<?php
/* =========================
 * File: api.php
 * Backend endpoints for listing, saving, exporting, and adding keys
 * ========================= */

declare( strict_types = 1 );

session_start();
header( 'Content-Type: application/json; charset=utf-8' );

require_once __DIR__ . '/resx_utils.php';
require_once __DIR__ . '/languages.php';

const RESX_DIR = __DIR__ . '/resx';
const RESX_BACKUP_DIR = __DIR__ . '/resx-backups';
const BASE_FILE = 'Resources.resx';
const FILE_STEM = 'Resources';
const CSRF_KEY = 'csrf_token';
const CONTRIBUTORS_FILE = __DIR__ . '/contributors.json';

$action = $_GET[ 'action' ] ?? '';

try
{
	switch ( $action )
	{
		case 'list':
			list_action();
			break;
		case 'save':
			save_action();
			break;
		case 'download_resx':
			download_resx_action();
			break;
		default:
			echo json_encode( [ 'ok' => false, 'error' => 'Unknown action' ] );
	}
}
catch ( Throwable $e )
{
	http_response_code( 500 );
	echo json_encode( [ 'ok' => false, 'error' => $e->getMessage() ] );
}

function list_action(): void
{
	header( 'Content-Type: application/json; charset=utf-8' );
	$lang = get_lang();
	$base = load_resx_safe( RESX_DIR . '/' . BASE_FILE );
	$locPath = RESX_DIR . '/' . FILE_STEM . ".{$lang}.resx";
	$loc = file_exists( $locPath ) ? load_resx_safe( $locPath ) : [];
	
	$rows = [];
	foreach ( $base as $name => $item )
	{
		$rows[] = [
			'id' => $item[ 'id' ] ?? '',
			'key' => $name,
			'comment' => $item[ 'comment' ] ?? '',
			'en' => $item[ 'value' ] ?? '',
			'value' => $loc[ $name ][ 'value' ] ?? '',
		];
	}
	
	usort( $rows, function ( $a, $b )
	{
		$ac = trim( (string) ( $a[ 'comment' ] ?? '' ) );
		$bc = trim( (string) ( $b[ 'comment' ] ?? '' ) );
		if ( $ac === '' && $bc !== '' ) return 1; // empty comments last
		if ( $ac !== '' && $bc === '' ) return -1; // non-empty comments first
		$cmp = strcasecmp( $ac, $bc );
		if ( $cmp !== 0 ) return $cmp;
		return strcasecmp( (string) ( $a[ 'key' ] ?? '' ), (string) ( $b[ 'key' ] ?? '' ) );
	} );
	
	echo json_encode( [ 'ok' => true, 'rows' => $rows ] );
}

function save_action(): void
{
	require_json();
	csrf_check();
	
	$payload = json_decode( file_get_contents( 'php://input' ), true ) ?? [];
	
	$lang = sanitize_lang( $payload[ 'lang' ] ?? '' );
	$key = trim( (string) ( $payload[ 'key' ] ?? '' ) );
	$val = (string) ( $payload[ 'value' ] ?? '' );
	$contributorName = trim( (string) ( $payload[ 'contributorName' ] ?? '' ) ); // NEW
	
	if ( $lang === '' || $key === '' )
	{
		throw new RuntimeException( 'Missing lang or key' );
	}
	
	$locPath = RESX_DIR . '/' . FILE_STEM . ".{$lang}.resx";
	$backupPath = RESX_BACKUP_DIR . '/' . FILE_STEM . ".{$lang}.resx";
	$basePath = RESX_DIR . '/' . BASE_FILE;
	
	$base = load_resx_safe( $basePath );
	
	// Ensure locale file exists & contains at least base keys
	$loc = file_exists( $locPath ) ? load_resx_safe( $locPath ) : [];
	foreach ( $base as $name => $item )
	{
		if ( !isset( $loc[ $name ] ) )
		{
			$loc[ $name ] = [
				'id' => $item[ 'id' ] ?? null,
				'comment' => $item[ 'comment' ] ?? null,
				'value' => ''
			];
		}
	}
	
	// Upsert only if changed
	if ( !isset( $loc[ $key ] ) )
	{
		$loc[ $key ] = [ 'id' => null, 'comment' => null, 'value' => '' ];
	}
	$old = (string) ( $loc[ $key ][ 'value' ] ?? '' );
	$changed = ( $old !== $val );
	
	$loc[ $key ][ 'value' ] = $val;
	
	// Save .resx (with backup)
	save_resx_safe( $locPath, $backupPath, $loc, $base );
	
	// If the translation actually changed and we got a contributor name, record it
	if ( $changed && $contributorName !== '' )
	{
		record_contributor_name( $lang, $contributorName ); // NEW
	}
	
	echo json_encode( [ 'ok' => true, 'changed' => $changed ] );
}

function download_resx_action(): void
{
	$lang = get_lang();
	$locPath = RESX_DIR . '/' . FILE_STEM . ".{$lang}.resx";
	
	if ( file_exists( $locPath ) )
	{
		header( 'Content-Type: application/xml; charset=UTF-8' );
		header( 'Content-Disposition: attachment; filename="' . basename( $locPath ) . '"' );
		header( 'Cache-Control: no-store, no-cache, must-revalidate, max-age=0' );
		
		echo file_get_contents( $locPath );
	}
	else
	{
		http_response_code( 404 );
		
		echo "RESX file for language {$lang} does not exist yet.";
	}
	
	exit;
}

function get_lang(): string
{
	$lang = sanitize_lang( $_GET[ 'lang' ] ?? '' );
	if ( $lang === '' ) throw new RuntimeException( 'Missing lang' );
	return $lang;
}

function sanitize_lang( string $s ): string
{
	return preg_replace( '~[^A-Za-z0-9_-]~', '', $s );
}

function require_json(): void
{
	if ( stripos( $_SERVER[ 'CONTENT_TYPE' ] ?? '', 'application/json' ) === false )
	{
		throw new RuntimeException( 'Expected JSON' );
	}
}

function csrf_check(): void
{
	if ( !hash_equals( $_SESSION[ CSRF_KEY ] ?? '', ( json_decode( file_get_contents( 'php://input' ), true )[ 'csrf' ] ?? '' ) ) ) throw new RuntimeException( 'CSRF failed' );
}

/**
 * Keep a per-language unique list of contributor names.
 * File format:
 * {
 *   "it-IT": ["Alice", "Carlo"],
 *   "nb-NO": ["Ola Nordmann"]
 * }
 */
function record_contributor_name( string $lang, string $name ): void
{
	$path = CONTRIBUTORS_FILE;
	
	// Create file if missing
	if ( !file_exists( $path ) )
	{
		@file_put_contents( $path, json_encode( new stdClass(), JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE ) );
	}
	
	$fp = @fopen( $path, 'c+' );
	if ( !$fp ) return;
	
	try
	{
		if ( !flock( $fp, LOCK_EX ) )
		{
			fclose( $fp );
			return;
		}
		
		$size = filesize( $path );
		$raw = $size > 0 ? fread( $fp, $size ) : '{}';
		$data = json_decode( $raw ?: '{}', true );
		if ( !is_array( $data ) ) $data = [];
		
		if ( !isset( $data[ $lang ] ) || !is_array( $data[ $lang ] ) ) $data[ $lang ] = [];
		
		$trimmed = trim( $name );
		if ( $trimmed !== '' )
		{
			$exists = false;
			foreach ( $data[ $lang ] as $n )
			{
				if ( mb_strtolower( trim( (string) $n ) ) === mb_strtolower( $trimmed ) )
				{
					$exists = true;
					break;
				}
			}
			if ( !$exists )
			{
				$data[ $lang ][] = $trimmed;
				ftruncate( $fp, 0 );
				rewind( $fp );
				fwrite( $fp, json_encode( $data, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE ) );
			}
		}
		
		flock( $fp, LOCK_UN );
	}
	finally
	{
		fclose( $fp );
	}
}
