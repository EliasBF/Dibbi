using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace Dibbi
{
    /*
    Estado del reproductor.
    Determina como se mostraran los widgets dentro del dialogo.
    */
    public enum PlayerState
    {
        Preview,
        NoteAudio
    }

    /*
    Dialogo de previsualización y reproducción de las notas de audio.
    */
    public class PlayerDialog : DialogFragment
    {
        /*
        Evento disparado al iniciar reproducción.
        Se dispara al hacer click en el icono de "play".
        */ 
        public event EventHandler PlayerStarting;

        /*
        Evento disparado al detener reproducción.
        Se dispara al hacer click en el icono de "pause".
        */
        public event EventHandler PlayerStoping;

        /*
        Evento disparado al cambiar el progreso de la reproducción.
        Se dispara al cambiar la ubicación de la barra de progreso.
        */
        public event EventHandler<PropertyEventArgs<int>> PlayerProgressChanged;

        /*
        Evento disparado al confirmar la operación del dialogo.
        Se dispara al hacer click en el boton "guardar".
        */
        public event EventHandler<PropertyEventArgs<NoteItem>> DialogConfirm;

        /*
        Evento disparado al rechazar la operación del dialogo.
        Se dispara al hacer click en el boton "descartar".
        */
        public event EventHandler DialogDismiss;

        /*
        Evento disparado al cerrarse el dialogo.
        Se dispara cuando el dialogo deja de ser visible.
        */
        public event EventHandler BaseDialogDismiss;

        /*
        Evento disparado cuando el dialogo es visible.
        Se dispara cada vez que se finaliza una grabación o al iniciar la reproducción de una nota de audio.
        */
        public event EventHandler DialogShow;

        // Widgets del dialogo.
        private ImageButton playerButton = null;
        private SeekBar playerSeek = null;
        private EditText playerDescription = null;

        private int Duration = 0;
        private bool _CanPlaying = true;
        private string _title;

        public PlayerState State { get; set; }
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                if (State == PlayerState.Preview)
                    return;

                _title = value;
            }
        }

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

                if (!_CanPlaying)
                {
                    EventHandler onPlayerStartingEvent = PlayerStarting;
                    onPlayerStartingEvent(this, null);
                }
                else
                {
                    EventHandler onPlayerStopingEvent = PlayerStoping;
                    onPlayerStopingEvent(this, null);
                }
            }
        }

        public static PlayerDialog NewInstance()
        {
            PlayerDialog dialog = new PlayerDialog();
            return dialog;
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            AlertDialog.Builder builder = new AlertDialog.Builder(Activity);
            LayoutInflater inflater = Activity.LayoutInflater;
            View view = inflater.Inflate(Resource.Layout.PlayerDialogLayout, null);

            playerButton = view.FindViewById<ImageButton>(Resource.Id.playButton);
            playerButton.Click += PlayerButtonClick;
            playerSeek = view.FindViewById<SeekBar>(Resource.Id.seekPlayer);
            playerSeek.ProgressChanged += PlayerSeekProgressChanged;
            playerDescription = view.FindViewById<EditText>(Resource.Id.descriptionText);

            builder.SetView(view);
            builder.SetPositiveButton("Guardar", (sender, e) => { });
            builder.SetNegativeButton("Descartar", (sender, e) => { });

            if (State == PlayerState.Preview)
                builder.SetTitle(Resource.String.PlayerDialogTitle);
            else if (State == PlayerState.NoteAudio)
                builder.SetTitle(Title);

            AlertDialog dialog = builder.Create();
            dialog.ShowEvent += OnShow;

            return dialog;
        }

        public override void OnStart()
        {
            base.OnStart();

            AlertDialog dialog = Dialog as AlertDialog;

            if (State == PlayerState.Preview)
            {
                Button positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                positiveButton.Click += ConfirmDialogClick;

                Button negativeButton = dialog.GetButton((int)DialogButtonType.Negative);
                negativeButton.Click += DismissDialogClick;
            }
            else if (State == PlayerState.NoteAudio)
            {
                Button positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                positiveButton.Visibility = ViewStates.Gone;

                Button negativeButton = dialog.GetButton((int)DialogButtonType.Negative);
                negativeButton.Visibility = ViewStates.Gone;

                playerDescription.Visibility = ViewStates.Gone;
            }
        }

        public override void OnDismiss(IDialogInterface dialog)
        {
            base.OnDismiss(dialog);

            if (!CanPlaying)
                CanPlaying = !CanPlaying;

            EventHandler onBaseDialogDismiss = BaseDialogDismiss;
            onBaseDialogDismiss(this, null);
        }

        private void ConfirmDialogClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(playerDescription.Text) || string.IsNullOrWhiteSpace(playerDescription.Text))
            {
                Toast.MakeText(Activity, "Falta una descripción.", ToastLength.Short).Show();
                return;
            }

            NoteItem note = new NoteItem() {
                CreatedAt = DateTime.Now,
                Description = playerDescription.Text,
                Duration = Methods.DurationToString(Duration),
                Filename = string.Format("{0}/{1}.3gp", Activity.FilesDir.AbsolutePath, playerDescription.Text)
            };

            PropertyEventArgs<NoteItem> eventArgs = new PropertyEventArgs<NoteItem>(note);
            EventHandler<PropertyEventArgs<NoteItem>> onDialogConfirm = DialogConfirm;
            onDialogConfirm(this, eventArgs);
            Dismiss();
        }

        private void DismissDialogClick(object sender, EventArgs e)
        {
            EventHandler onDialogDismiss = DialogDismiss;
            onDialogDismiss(this, null);
            Dismiss();
        }

        private void PlayerButtonClick(object sender, EventArgs e)
        {
            CanPlaying = !CanPlaying;
        }

        private void PlayerSeekProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            if (e.FromUser)
            {
                PropertyEventArgs<int> eventArgs = new PropertyEventArgs<int>(e.Progress);
                EventHandler<PropertyEventArgs<int>> onPlayerProgressChangedEvent = PlayerProgressChanged;
                onPlayerProgressChangedEvent(this, eventArgs);
            }
        }

        public void PrepareSeekPlayer(int length)
        {
            if (playerSeek.Max == length)
                return;

            playerSeek.Max = length;
            playerSeek.Progress = 0;
            Duration = length;
        }

        public void UpdateSeekPlayer(int progress)
        {
            if (CanPlaying)
                return;

            playerSeek.Progress = progress;

            if (progress == 0)
            {
                playerButton.SetImageResource(Resource.Drawable.ic_play_arrow_black_36dp);
                CanPlaying = !CanPlaying;
            }
        }

        private void OnShow(object sender, EventArgs e)
        {
            EventHandler onDialogShowEvent = DialogShow;
            onDialogShowEvent(this, null);
        }

        public void SetPlayerIcon(int resource)
        {
            playerButton.SetImageResource(resource);
        }
    }
}