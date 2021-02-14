

       
       //Userd for key event check listing files and directories to be suggested for autocomplete
        private static void KeyDown(KeyEventArgs e)
        {


            //test : output key string format
            //Console.WriteLine(e.KeyCode);
            //-------------------------

            //we read every char in line
            keyStrokes = e.KeyCode.ToString();

            //add chars to list
            if (e.KeyCode != Keys.Delete && e.KeyCode != Keys.Back && e.KeyCode != Keys.Enter && e.KeyCode != Keys.Capital && e.KeyCode != Keys.Left && e.KeyCode != Keys.Right)
                listChars.Add(keyStrokes);

            //compbine chars
            string outPutChars = string.Join("", listChars);

            //clean the output for space and tab
            outPutChars = outPutChars.Replace("Space", " ");
            outPutChars = outPutChars.Replace("LShiftKey", "");



            //check if keycode is PageUp key presed and use for execution command
            if (e.KeyCode == Keys.PageUp)
            {
                //read current location
                string cDir = File.ReadAllText(FileSystem.CurrentLocation);

                if (Directory.Exists(cDir))
                {
                    //get list of files and directoriles
                    var files = Directory.GetFiles(cDir);
                    var directories = Directory.GetDirectories(cDir);
                                       //---------------------------

                    foreach (var dir in directories)
                    {
                        //increment counter for indexing directories
                        countList++;

                        //replace separator for spliting (seems is illegal if I use normal on)
                        string d = dir.Replace(@"\", "/");

                        //we count the number of separators
                        MatchCollection matchDir = Regex.Matches(d, "/");
                        int parseDir = matchDir.Count;

                        //we split every line by separator
                        string[] dirs = d.Split('/');

                        //we add to list the last part
                        listCurrentDir.Add(dirs[parseDir]);

                    }
                    foreach (var file in files)
                    {
                        //increment counter for indexing files
                        countList++;

                        //replace separator for spliting (seems is illegal if I use normal on)
                        string f = file.Replace(@"\", "/");

                        //we count the number of separators
                        MatchCollection matchDir = Regex.Matches(f, @"/");
                        int parseFile = matchDir.Count;

                        //we split every line by separator
                        string[] fS = f.Split('/');

                        //we add to list the last part
                        listCurrentDir.Add(fS[parseFile]);
                    }
                }
                else
                {
                    Console.Write($"Directory '{cDir}' dose not exist!");
                }


                //we increment the PageUp key execution
                cList++;

                //check if index of files/directories is bigger than key execution count
                if (countList >= cList)
                {

                    //save the next lines from list
                    line = listCurrentDir.Skip(cList - 1).Take(1).First();


                    Console.WriteLine(line);
                }
                else
                {
                    //clear PaguUp key execution counter if is bigger than countList
                    cList = 0;

                    //clear index counter
                    countList = 0;


                    line = listCurrentDir.Skip(cList - 1).Take(1).First();


                    //we clear the list
                    listChars.Clear();
                    listCurrentDir.Clear();
                    //----------------
                }



                e.Handled = false;
            }
        }
       
        /// <summary>
        /// On press key intercept
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Woker_DoWork(object sender, DoWorkEventArgs e)
        {
            InterceptKeys.SetupHook(KeyDown);
            InterceptKeys.ReleaseHook();


        }
       