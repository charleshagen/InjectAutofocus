# Inject Autofocus

Add the InjectAutofocus trigger into your sequence. When evaluated, the trigger will see if an autofocus routine has been requested and will run
an autofocus routine before the next exposure. If another autofocus event occurs before your requested AF routine has started, it will clear the request
only running once. You can request an autofocus routine while the sequence is running via the 'Inject Autofocus' panel and associated button in the imaging tab.