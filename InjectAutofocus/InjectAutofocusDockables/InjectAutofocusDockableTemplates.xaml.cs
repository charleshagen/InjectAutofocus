﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CharlesHagen.NINA.InjectAutofocus.InjectAutofocusDockables {
    [Export(typeof(ResourceDictionary))]
    public partial class MyPluginDockableTemplates : ResourceDictionary {
        public MyPluginDockableTemplates() {
            InitializeComponent();
        }
    }
}