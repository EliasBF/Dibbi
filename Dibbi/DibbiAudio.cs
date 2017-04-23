using System;
using Android.Media;
using Android.OS;

namespace Dibbi
{
    /*
    Acceso a las APIs para reproducir y grabar audio.
    */
    public class DibbiAudio
    {
        // Evento emitido al iniciar grabación.
        public event EventHandler<PropertyEventArgs<bool>> OnRecording;
        // Evento emitido al iniciar reproducción.
        public event EventHandler<PropertyEventArgs<bool>> OnPlaying;
        // Evento emitido al detener grabación.
        public event EventHandler<PropertyEventArgs<bool>> OnStopRecording;
        // Evento emitido al detener reproducción.
        public event EventHandler<PropertyEventArgs<bool>> OnStopPlaying;
        //Evento emitido al cambiar el progreso de la reproducción.
        public event EventHandler<PropertyEventArgs<int>> OnProgressPlaying;
        // Evento emitido cada segundo mientras se esta grabando.
        public event EventHandler<PropertyEventArgs<int>> OnUpdateRecordTime;

        // Ruta al archivo en donde se graban los audios.
        private string CacheFileName = null;

        // Estado del grabador.
        private bool _CanRecording = true;
        // Estado del reproductor.
        private bool _CanPlaying = true;
        // Duración de la grabación.
        private int _RecordDuration = 0;

        // Grabador
        private MediaRecorder Recorder = null;
        // Reproductor
        private MediaPlayer Player = null;

        // Actualizadores de progreso.
        private Handler updateHandler = new Handler();
        private Handler recordTimeHandler = new Handler();

        /*
        Estado del grabador.

            ~ Cada vez que cambia, se emite un evento de notificación con el valor del estado.
        */
        private bool CanRecording
        {
            get
            {
                return _CanRecording;
            }
            set
            {
                if (_CanRecording == value)
                    return;

                _CanRecording = value;

                PropertyEventArgs<bool> eventArgs = new PropertyEventArgs<bool>(_CanRecording);

                if (!_CanRecording)
                {
                    EventHandler<PropertyEventArgs<bool>> onRecordingEvent = OnRecording;
                    onRecordingEvent(this, eventArgs);
                }
                else
                {
                    EventHandler<PropertyEventArgs<bool>> onStopRecordingEvent = OnStopRecording;
                    onStopRecordingEvent(this, eventArgs);
                }
            }
        }

        /*
        Estado del reproductor.

            ~ Cada vez que cambia, se emite un evento de notificación con el valor del estado.
        */
        private bool CanPlaying
        {
            get
            {
                return _CanPlaying;
            }

            set
            {
                if (_CanPlaying == value)
                    return;

                _CanPlaying = value;

                PropertyEventArgs<bool> eventArgs = new PropertyEventArgs<bool>(_CanPlaying);

                if (!_CanPlaying)
                {
                    EventHandler<PropertyEventArgs<bool>> onPlayingEvent = OnPlaying;
                    onPlayingEvent(this, eventArgs);
                }
                else
                {
                    EventHandler<PropertyEventArgs<bool>> onStopPlayingEvent = OnStopPlaying;
                    onStopPlayingEvent(this, eventArgs);
                }
            }
        }

        public string FileName
        {
            get
            {
                return CacheFileName;
            }

            set
            {
                if (CacheFileName == value)
                    return;

                CacheFileName = value;
            }
        }

        /*
        Duración de la reproducción en la cola de reproducción.

            ~ Solo de lectura.
        */
        public int PlayerDuration
        {
            get
            {
                if (Player != null)
                    return Player.Duration;
                else
                    return 0;
            }
        }

        /*
        Duración actualizada de la grabación en progreso.

            ~ Cada vez que cambia se emite un evento con el valor actualizado.
        */
        private int RecordDuration
        {
            get
            {
                return _RecordDuration;
            }

            set
            {
                if (_RecordDuration == value)
                    return;

                _RecordDuration = value;

                if (_RecordDuration != 0)
                {
                    PropertyEventArgs<int> eventArgs = new PropertyEventArgs<int>(_RecordDuration);

                    EventHandler<PropertyEventArgs<int>> onUpdateRecordTimeEvent = OnUpdateRecordTime;
                    onUpdateRecordTimeEvent(this, eventArgs);
                }
            }
        }

        ~DibbiAudio()
        {
            if (Player != null)
            {
                Player.Release();
                Player.Dispose();
            }

            if (Recorder != null)
            {
                Recorder.Release();
                Recorder.Dispose();
            }

            updateHandler.RemoveCallbacks(UpdateProgressPlayer);
            updateHandler.Dispose();

            recordTimeHandler.RemoveCallbacks(UpdateTimeRecord);
            recordTimeHandler.Dispose();
        }

        private void StartRecord()
        {
            Recorder = new MediaRecorder();

            Recorder.SetAudioSource(AudioSource.Mic);
            Recorder.SetOutputFormat(OutputFormat.ThreeGpp);
            Recorder.SetOutputFile(FileName);
            Recorder.SetAudioEncoder(AudioEncoder.AmrNb);

            Recorder.Prepare();
            Recorder.Start();

            // Iniciando actualizador para tiempo de grabación.
            recordTimeHandler.PostDelayed(UpdateTimeRecord, 1000);

            CanRecording = !CanRecording;
        }

        private void StopRecord()
        {
            Recorder.Stop();
            Recorder.Release();
            Recorder.Dispose();
            Recorder = null;

            RecordDuration = 0;
            recordTimeHandler.RemoveCallbacks(UpdateTimeRecord);

            CanRecording = !CanRecording;
        }

        public void PreparePlay(string audioFilename = null)
        {
            Player = new MediaPlayer();

            Player.Completion += OnPlayerCompletion;

            if (audioFilename == null)
                Player.SetDataSource(FileName);
            else
                Player.SetDataSource(audioFilename);

            Player.Prepare();
        }

        private void StartPlay()
        {
            Player.Start();

            // Iniciando actualizador para tiempo de reproducción.
            updateHandler.PostDelayed(UpdateProgressPlayer, 100);

            CanPlaying = !CanPlaying;
        }

        private void StopPlay()
        {
            Player.Pause();
            updateHandler.RemoveCallbacks(UpdateProgressPlayer);
            CanPlaying = !CanPlaying;
        }

        public void freePlay()
        {
            Player.Stop();
            Player.Release();
            Player.Completion -= OnPlayerCompletion;
            Player.Dispose();
            Player = null;

            if (!CanPlaying)
                CanPlaying = !CanPlaying;
        }

        public void PingRecorder()
        {
            if (CanRecording)
                StartRecord();
            else
                StopRecord();
        }

        public void PingPlayer()
        {
            if (CanPlaying)
                StartPlay();
            else
                StopPlay();
        }

        private void UpdateProgressPlayer()
        {
            if (Player == null)
                return;

            if (!Player.IsPlaying)
                return;

            PropertyEventArgs<int> eventArgs = new PropertyEventArgs<int>(Player.CurrentPosition);
            EventHandler<PropertyEventArgs<int>> onProgressPlayingEvent = OnProgressPlaying;
            onProgressPlayingEvent(this, eventArgs);

            // Reiniciando actualizador para tiempo de reproducción.
            updateHandler.PostDelayed(UpdateProgressPlayer, 100);
        }

        private void UpdateTimeRecord()
        {
            if (Recorder == null)
                return;

            RecordDuration += 1;

            // Reiniciando actualizador para tiempo de grabación.
            recordTimeHandler.PostDelayed(UpdateTimeRecord, 1000);
        }

        public void ChangePlayerPosition(int position)
        {
            Player.SeekTo(position);
        }

        private void OnPlayerCompletion(object sender, EventArgs e)
        {
            updateHandler.RemoveCallbacks(UpdateProgressPlayer);

            PropertyEventArgs<int> eventArgs = new PropertyEventArgs<int>(0);
            EventHandler<PropertyEventArgs<int>> onProgressPlayingEvent = OnProgressPlaying;
            onProgressPlayingEvent(this, eventArgs);

            if (!CanPlaying)
                CanPlaying = !CanPlaying;
        }
    }
}