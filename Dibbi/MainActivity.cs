using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Content;
using Android.Views;
using Android.Widget;
using Android.Util;

namespace Dibbi
{
    [
        Activity(Label = "@string/MainTitle",
        Icon = "@drawable/Icon",
        Theme = "@style/DibbiTheme.Main")
    ]
    public class MainActivity : Activity
    {
        // Componentes de Dibbi
        private DibbiAudio DibbiAudio;
        private PlayerDialog DibbiPlayer;
        private DibbiData DibbiData;

        private List<NoteItem> Notes = null;
        private NoteListAdapter NotesAdapter = null;

        // UI Widgets.
        private ListView NotesList = null;
        private Toolbar BottomToolbar = null;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);

            // Preparando UI.
            NotesList = FindViewById<ListView>(Resource.Id.ListNotes);

            BottomToolbar = FindViewById<Toolbar>(Resource.Id.MainBottomToolbar);
            BottomToolbar.InflateMenu(Resource.Menu.main_bottom_menu);
            BottomToolbar.MenuItemClick += BottomToolbarItemClick;
            BottomToolbar.FindViewById(Resource.Id.menuStop).Visibility = ViewStates.Gone;

            SetActionBar(FindViewById<Toolbar>(Resource.Id.MainTopToolbar));
            RegisterForContextMenu(NotesList);

            // Inicializando componentes.
            DibbiAudio = new DibbiAudio();
            DibbiAudio.FileName = CacheDir.AbsolutePath + "/cacheaudiorecord.3gp";
            DibbiAudio.OnPlaying += OnPlay;
            DibbiAudio.OnStopPlaying += OnPlay;
            DibbiAudio.OnRecording += OnRecord;
            DibbiAudio.OnStopRecording += OnRecord;
            DibbiAudio.OnProgressPlaying += OnProgress;
            DibbiAudio.OnUpdateRecordTime += OnRecordTime;

            DibbiPlayer = PlayerDialog.NewInstance();
            DibbiPlayer.PlayerStarting += OnPlayerStarting;
            DibbiPlayer.PlayerStoping += OnPlayerStoping;
            DibbiPlayer.PlayerProgressChanged += OnPlayerProgressChanged;
            DibbiPlayer.DialogConfirm += OnDialogConfirm;
            DibbiPlayer.DialogDismiss += OnDialogDismiss;
            DibbiPlayer.BaseDialogDismiss += OnBaseDialogDismiss;
            DibbiPlayer.DialogShow += OnPlayerShow;

            DibbiData = new DibbiData(FilesDir.AbsolutePath + "/dibbi.db");
        }

        protected async override void OnResume() {
            base.OnResume();

            // Cargando notas.
            if (Notes == null)
            {
                ProgressDialog dialog = new ProgressDialog(this);
                dialog.SetMessage("Cargando notas");
                dialog.SetCancelable(false);
                dialog.SetCanceledOnTouchOutside(false);
                dialog.Show();

                Notes = await DibbiData.GetNotes();
                NotesAdapter = new NoteListAdapter(this, Notes);
                NotesList.Adapter = NotesAdapter;
                NotesList.EmptyView = FindViewById(Resource.Id.MainListEmptyDescription);
                NotesList.FastScrollEnabled = true;
                NotesList.ItemClick += ListViewItemClick;
                NotesList.Scroll += OnNotesListScroll;
                NotesList.DividerHeight = 1;

                await Task.Delay(1000);

                dialog.Dismiss();
                dialog.Dispose();
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main_top_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override void OnCreateContextMenu(IContextMenu menu, View view, IContextMenuContextMenuInfo menuInfo)
        {
            if (view.Id == Resource.Id.ListNotes)
            {
                AdapterView.AdapterContextMenuInfo info = menuInfo as AdapterView.AdapterContextMenuInfo;
                menu.SetHeaderTitle(Notes[info.Position].Description);
                menu.Add(Menu.None, 0, 0, "Reproducir");
                menu.Add(Menu.None, 1, 1, "Eliminar");
            }

            base.OnCreateContextMenu(menu, view, menuInfo);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int actionId = item.ItemId;

            if (actionId == Resource.Id.menuExit)
            {
                FinishAffinity();
            }

            return base.OnOptionsItemSelected(item);
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            AdapterView.AdapterContextMenuInfo info = item.MenuInfo as AdapterView.AdapterContextMenuInfo;
            int actionId = item.ItemId;
            NoteItem note = Notes[info.Position];

            // Opción: Reproducir
            if (actionId == 0)
            {
                DibbiAudio.PreparePlay(note.Filename);
                DibbiPlayer.State = PlayerState.NoteAudio;
                DibbiPlayer.Title = note.Description;
                DibbiPlayer.Show(FragmentManager, "player_dialog");
            }
            // Opción: Eliminar
            if (actionId == 1)
            {
                // Eliminar nota de la base de datos.
                DibbiData.removeNote(note);

                // Eliminar de la lista
                Notes.Remove(note);
                NotesAdapter.NotifyDataSetChanged();

                Toast.MakeText(this, "Nota eliminada", ToastLength.Short).Show();
            }

            return base.OnContextItemSelected(item);
        }

        private void ListViewItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            NoteItem note = Notes[e.Position];

            DibbiAudio.PreparePlay(note.Filename);
            DibbiPlayer.State = PlayerState.NoteAudio;
            DibbiPlayer.Show(FragmentManager, "player_dialog");
        }

        private void BottomToolbarItemClick(object sender, Toolbar.MenuItemClickEventArgs e)
        {
            // Iniciando o deteniendo grabación.
            DibbiAudio.PingRecorder();
        }

        private async void OnRecord(object sender, PropertyEventArgs<bool> args)
        {
            // Deteniendo grabación (Grabación terminada).
            if (args.Property)
            {
                // Actualizando UI.
                BottomToolbar.FindViewById(Resource.Id.menuRecord).Visibility = ViewStates.Visible;
                BottomToolbar.FindViewById(Resource.Id.menuStop).Visibility = ViewStates.Gone;

                // Preparando Reproductor
                DibbiAudio.PreparePlay();

                // Preparando y mostrando dialogo de reproducción.
                DibbiPlayer.State = PlayerState.Preview;
                DibbiPlayer.Show(FragmentManager, "player_dialog");

                // Ocultado detalles de la ultima grabación.
                NotesList.Visibility = ViewStates.Visible;
                FindViewById<LinearLayout>(Resource.Id.RecordDescription).Visibility = ViewStates.Gone;
                FindViewById<TextView>(Resource.Id.RecordDuration).Text = "0:00";

                NotesList.Alpha = 1.0f;
            }
            // Iniciando grabación.
            else
            {
                // Actualizando UI.
                BottomToolbar.FindViewById(Resource.Id.menuRecord).Visibility = ViewStates.Gone;
                BottomToolbar.FindViewById(Resource.Id.menuStop).Visibility = ViewStates.Visible;

                NotesList.Animate().Alpha(0.0f).SetDuration(400);
                await Task.Delay(400);

                // Mostrar detalles de la grabación.
                NotesList.Visibility = ViewStates.Gone;
                FindViewById<LinearLayout>(Resource.Id.RecordDescription).Visibility = ViewStates.Visible;
            }
        }

        private void OnPlay(object sender, PropertyEventArgs<bool> args)
        {
            // Deteniendo reproducción.
            if (args.Property)
            {
                DibbiPlayer.SetPlayerIcon(Resource.Drawable.ic_play_arrow_black_36dp);
            }
            // Iniciando reproduccción.
            else
            {
                DibbiPlayer.SetPlayerIcon(Resource.Drawable.ic_pause_black_36dp);
            }
        }

        private void OnProgress(object sender, PropertyEventArgs<int> args)
        {
            // Actualizando la barra de progreso del reproductor en el dialogo.
            DibbiPlayer.UpdateSeekPlayer(args.Property);
        }

        private void OnRecordTime(object sender, PropertyEventArgs<int> args)
        {
            // Actualizando el tiempo de grabación actual en los detalles de la grabación.
            FindViewById<TextView>(Resource.Id.RecordDuration).Text = Methods.DurationToTimeClockString(args.Property);
        }

        private void OnPlayerStarting(object sender, EventArgs e)
        {
            // Cambiando el estado del reproductor.
            DibbiAudio.PingPlayer();
        }

        private void OnPlayerStoping(object sender, EventArgs e)
        {
            // Cambiando el estado del reproductor.
            DibbiAudio.PingPlayer();
        }

        private void OnPlayerProgressChanged(object sender, PropertyEventArgs<int> e)
        {
            // Cambiando la posición de reproducción.
            DibbiAudio.ChangePlayerPosition(e.Property);
        }

        private async void OnDialogConfirm(object sender, PropertyEventArgs<NoteItem> e)
        {
            // Guardar audio en directorio local de la aplicacion.
            Stream fos = null;
            using (fos = OpenFileOutput(e.Property.Description + ".3gp", FileCreationMode.Private))
            {
                byte[] noteAudio = File.ReadAllBytes(DibbiAudio.FileName);
                fos.Write(noteAudio, 0, noteAudio.Length);
            }

            // Guardar datos de la nota en la base de datos.
            NoteItem note = await DibbiData.addNote(e.Property);

            // Añadir nota a la lista.
            Notes.Insert(0, note);
            NotesAdapter.NotifyDataSetChanged();

            Toast.MakeText(this, "Nota guardada", ToastLength.Short).Show();

            // Mostrar el inicio de la lista (notas se añaden al comienzo).
            NotesList.SetSelection(0);
        }

        private void OnDialogDismiss(object sender, EventArgs e)
        {
            Toast.MakeText(this, "Nota descartada", ToastLength.Short).Show();
        }

        private void OnBaseDialogDismiss(object sender, EventArgs e)
        {
            // Liberando el reproductor.
            DibbiAudio.freePlay();
        }

        private void OnPlayerShow(object sender, EventArgs e)
        {
            // Estableciendo el tamaño de la barra de progreso del reproductor en el dialogo.
            DibbiPlayer.PrepareSeekPlayer(DibbiAudio.PlayerDuration);
        }

        private async void OnNotesListScroll(object sender, AbsListView.ScrollEventArgs e)
        {
            // Prevenir cargar notas precipitadamente.
            NotesList.Scroll -= OnNotesListScroll;

            try
            {
                // Cargar notas solo si se ha llegado al final de la lista.
                bool more = e.FirstVisibleItem + e.VisibleItemCount >= e.TotalItemCount;

                if (more)
                {
                    ProgressDialog dialog = new ProgressDialog(this);
                    dialog.SetMessage("Cargando notas");
                    dialog.SetCancelable(false);
                    dialog.SetCanceledOnTouchOutside(false);
                    dialog.Show();

                    List<NoteItem> notes = await DibbiData.GetNotes(Notes[Notes.Count - 1].CreatedAt);
                    Notes.AddRange(notes);
                    NotesAdapter.NotifyDataSetChanged();

                    // Cargar más notas posteriormente solo si quedan más.
                    if (notes.Count == 20)
                        NotesList.Scroll += OnNotesListScroll;

                    await Task.Delay(1000);

                    dialog.Dismiss();
                    dialog.Dispose();
                }
                else
                    NotesList.Scroll += OnNotesListScroll;
            }
            catch (Exception err)
            {
                throw err;
            }
        }
    }
}