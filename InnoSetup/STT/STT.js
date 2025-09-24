
const headTitleElement = document.getElementById( 'headTitle' );
const bodyTitleElement = document.getElementById( 'bodyTitle' );
const hintElement = document.getElementById( 'hint' );
const micSelectElement = document.getElementById( 'micSelect' );
const enableElement = document.getElementById( 'enable' );

const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;

let speechRecognition = null;
let webSocket = null;
let language = 'en-US';

let mediaStream = null;
let micDeviceId = localStorage.getItem( 'maira.micDeviceId' ) || '';

let audioContext = null;
let sourceNode = null;
let unityGainNode = null;

function post( type, data = {} )
{
    if ( webSocket && webSocket.readyState === WebSocket.OPEN )
    {
        let json = JSON.stringify( { type, ...data } );

        console.log( json );

        webSocket.send( json );
    }
}

function wsUrl()
{
    const { protocol, host } = window.location;

    return ( protocol === 'https:' ? 'wss:' : 'ws:' ) + '//' + host + '/ws';
}

let strings = {
    title: 'MAIRA STT Bridge',
    hint: 'Choose an audio capture device, then click the enable button.',
    button: 'Enable Speech-To-Text'
};

function applyStrings()
{
    headTitleElement.textContent = strings.title;
    bodyTitleElement.textContent = strings.title;
    hintElement.textContent = strings.hint;
    enableElement.textContent = strings.button;
}

function supportedAudioConstraints()
{
    try
    {
        return navigator.mediaDevices.getSupportedConstraints?.() ?? {};
    }
    catch
    {
        return {};
    }
}

function makeMicConstraints( deviceId )
{
    const base = {
        ...( deviceId ? { deviceId: { exact: deviceId } } : {} ),
        autoGainControl: false,
        echoCancellation: false,
        noiseSuppression: false,
        channelCount: 1,
        advanced: [ {
            googAutoGainControl: false,
            googAutoGainControl2: false,
            googEchoCancellation: false,
            googNoiseSuppression: false
        } ]
    };

    const sup = supportedAudioConstraints();

    for ( const k of [ 'autoGainControl', 'echoCancellation', 'noiseSuppression', 'channelCount' ] )
    {
        if ( !sup[ k ] )
        {
            delete base[ k ];
        }
    }

    return { audio: base, video: false };
}

async function logTrackSettings( stream )
{
    try
    {
        const track = stream?.getAudioTracks?.()[ 0 ];

        if ( !track ) return;

        const settings = track.getSettings?.() || {};

        console.info( '[MAIRA STT] Mic settings negotiated:', settings );
    }
    catch
    {
    }
}

async function attachUnityGain( stream )
{
    try
    {
        if ( !stream ) return;

        const AudioContext = window.AudioContext || window.webkitAudioContext;

        if ( !AudioContext ) return;

        audioContext = audioContext || new AudioContext();

        try { sourceNode?.disconnect(); } catch { }
        try { unityGainNode?.disconnect(); } catch { }

        const inputNode = audioContext.createMediaStreamSource( stream );
        const gainNode = audioContext.createGain();

        gainNode.gain.value = 1.0;

        inputNode.connect( gainNode ).connect( audioContext.destination );

        sourceNode = inputNode;
        unityGainNode = gainNode;
    }
    catch
    {
    }
}

async function reassertNoAgcOnExistingTrack()
{
    try
    {
        const track = mediaStream?.getAudioTracks?.()[ 0 ];

        if ( !track?.applyConstraints ) return;

        const { audio } = makeMicConstraints( micDeviceId );
        const { deviceId, advanced, ...standardOnly } = audio;

        await track.applyConstraints( standardOnly );
        await logTrackSettings( mediaStream );
    }
    catch
    {
    }
}

function ensureWs()
{
    if ( webSocket && ( webSocket.readyState === WebSocket.OPEN || webSocket.readyState === WebSocket.CONNECTING ) ) return;

    webSocket = new WebSocket( wsUrl() );

    webSocket.onclose = () =>
    {
        setTimeout( ensureWs, 1000 );
    };

    webSocket.onmessage = ( ev ) =>
    {
        try
        {
            const msg = JSON.parse( ev.data );

            console.log( msg );

            if ( msg.type === 'setlanguage' )
            {
                if ( msg.language )
                {
                    applyLanguage( msg.language );
                }
            }
            else if ( msg.type === 'setstrings' )
            {
                strings = { ...strings, ...msg.strings };

                applyStrings();
            }
            else if ( msg.type === 'shutdown' )
            {
                destroyRecognizer();
                stopMediaStream();

                window.close();
            }
        }
        catch
        {
        }
    };
}

ensureWs();

async function refreshDevices()
{
    try
    {
        await navigator.mediaDevices.getUserMedia( makeMicConstraints( micDeviceId ) );
    }
    catch
    {
    }

    const devices = await navigator.mediaDevices.enumerateDevices();
    const mics = devices.filter( d => d.kind === 'audioinput' );

    micSelectElement.innerHTML = '';

    for ( const d of mics )
    {
        const opt = document.createElement( 'option' );

        opt.value = d.deviceId;
        opt.text = d.label || ( 'Microphone ' + ( micSelectElement.length + 1 ) );

        if ( micDeviceId && d.deviceId === micDeviceId ) opt.selected = true;

        micSelectElement.appendChild( opt );
    }

    if ( !micDeviceId && mics.length )
    {
        micDeviceId = mics[ 0 ].deviceId;
    }
}

navigator.mediaDevices.addEventListener( 'devicechange', refreshDevices );
document.addEventListener( 'DOMContentLoaded', refreshDevices );

async function switchMic( deviceId )
{
    micDeviceId = deviceId || '';

    localStorage.setItem( 'maira.micDeviceId', micDeviceId );

    stopMediaStream();

    enableElement.classList.remove( 'hidden' );

    try
    {
        const constraints = makeMicConstraints( micDeviceId );

        mediaStream = await navigator.mediaDevices.getUserMedia( constraints );

        await logTrackSettings( mediaStream );
        await attachUnityGain( mediaStream );
        await reassertNoAgcOnExistingTrack();
    }
    catch ( e )
    {
        console.warn( '[MAIRA STT] mic open failed:', e );
    }
}

micSelectElement.addEventListener( 'change', () => switchMic( micSelectElement.value ) );

function stopMediaStream()
{
    if ( mediaStream )
    {
        for ( const t of mediaStream.getTracks() )
        {
            try
            {
                t.stop();
            }
            catch
            {
            }
        }

        mediaStream = null;
    }

    try
    {
        sourceNode?.disconnect();
    }
    catch
    {
    }

    try
    {
        unityGainNode?.disconnect();
    }
    catch
    {
    }

    sourceNode = null;
    unityGainNode = null;
}

function createAndStartRecognizer()
{
    if ( !SpeechRecognition )
    {
        post( 'error', { message: 'SpeechRecognition not supported in this browser.' } );

        return null;
    }

    destroyRecognizer();

    speechRecognition = new SpeechRecognition();

    speechRecognition.lang = language;
    speechRecognition.interimResults = true;
    speechRecognition.continuous = true;
    speechRecognition.maxAlternatives = 1;

    speechRecognition.onresult = ( e ) =>
    {
        let partial = '', final = '';

        for ( let i = e.resultIndex; i < e.results.length; i++ )
        {
            const r = e.results[ i ];

            if ( r.isFinal )
            {
                final += r[ 0 ].transcript;
            }
            else
            {
                partial += r[ 0 ].transcript;
            }
        }

        if ( partial )
        {
            post( 'stt', { partial } );
        }

        if ( final )
        {
            post( 'stt', { final } );
        }
    };

    speechRecognition.onerror = ( ev ) => post( 'error', { message: ev.error || 'unknown' } );

    speechRecognition.onend = () =>
    {
        post( 'end' );

        setTimeout( () => { try { speechRecognition.start(); } catch { } }, 60 );
    };

    if ( !mediaStream )
    {
        navigator.mediaDevices.getUserMedia( makeMicConstraints( micDeviceId ) )
            .then( async ( s ) =>
            {
                mediaStream = s;

                await logTrackSettings( mediaStream );
                await attachUnityGain( mediaStream );
                await reassertNoAgcOnExistingTrack();
            } )
            .catch( () => { } );
    }
    else
    {
        reassertNoAgcOnExistingTrack();
    }

    try
    {
        speechRecognition.start();
    }
    catch ( e )
    {
        if ( !String( e ).includes( 'start' ) )
        {
            post( 'error', { message: e.message || 'start() failed' } );
        }
    }

    enableElement.classList.add( 'hidden' );
}

function destroyRecognizer()
{
    if ( speechRecognition )
    {
        try
        {
            speechRecognition.abort();
        }
        catch
        {
        }

        speechRecognition = null;
    }
}

function applyLanguage( newLanguage )
{
    if ( !newLanguage || ( newLanguage === language ) ) return;

    language = newLanguage;

    createAndStartRecognizer();
}

enableElement.addEventListener( 'click', async () =>
{
    try
    {
        await switchMic( micSelectElement.value );

        createAndStartRecognizer();
    }
    catch ( e )
    {
    }
} );
